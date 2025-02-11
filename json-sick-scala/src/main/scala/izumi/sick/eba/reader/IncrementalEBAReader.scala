package izumi.sick.eba.reader

import io.circe.Json
import izumi.sick.eba.reader.incremental.{IncrementalJValue, IncrementalTableFixed, IncrementalTableVar, OneObjTable}
import izumi.sick.eba.writer.codecs.EBACodecs.{DebugTableName, IntCodec, RefCodec, ShortCodec}
import izumi.sick.model.*

import java.io.{BufferedInputStream, ByteArrayInputStream, DataInputStream, InputStream}
import java.nio.file.{Files, Path}
import scala.collection.immutable.ArraySeq
import scala.collection.mutable
import scala.util.Try

object IncrementalEBAReader {

  def openFile(path: Path, inMemoryThreshold: Long = 65536, eagerOffsets: Boolean = true): IncrementalEBAReader = {
    if (Files.size(path) <= inMemoryThreshold) {
      openBytes(Files.readAllBytes(path))
    } else {
      val is = new BufferedInputStream(Files.newInputStream(path))
      open(is, eagerOffsets)
    }
  }

  def openBytes(bytes: Array[Byte], eagerOffsets: Boolean = true): IncrementalEBAReader = {
    val is = new ByteArrayInputStream(bytes)
    open(is, eagerOffsets)
  }

  /**
    * @param inputStream must support `mark`/`reset` (e.g. `BufferedInputStream` or `ByteArrayInputStream`)
    */
  def open(inputStream: InputStream, eagerOffsets: Boolean = true): IncrementalEBAReader = {
    if (!inputStream.markSupported()) {
      throw new IllegalArgumentException(s"Cannot read EBA incrementally from a non-seekable inputStream=$inputStream")
    }

    val it = new DataInputStream(inputStream)
    it.mark(Int.MaxValue)

    val version = IntCodec.decode(it)
    val expectedVersion = 0
    require(version == expectedVersion, s"SICK version expected to be $expectedVersion, got $version")

    val tableCount = IntCodec.decode(it)
    val expectedTableCount = 10
    require(tableCount == expectedTableCount, s"SICK table count expected to be $expectedTableCount, got $tableCount")

    val offsets = (0 until tableCount).iterator.map(_ => IntCodec.decode(it).toLong).to(ArraySeq)
    val objectIndexBucketCount = ShortCodec.decode(it)

    val intTable: IncrementalTableFixed[Int] = IncrementalTableFixed.allocate[Int](it, offsets(0))
    val longTable: IncrementalTableFixed[Long] = IncrementalTableFixed.allocate[Long](it, offsets(1))
    val bigIntTable: IncrementalTableVar[BigInt] = IncrementalTableVar.allocate[BigInt](it, offsets(2), eagerOffsets)

    val floatTable: IncrementalTableFixed[Float] = IncrementalTableFixed.allocate[Float](it, offsets(3))
    val doubleTable: IncrementalTableFixed[Double] = IncrementalTableFixed.allocate[Double](it, offsets(4))
    val bigDecTable: IncrementalTableVar[BigDecimal] = IncrementalTableVar.allocate[BigDecimal](it, offsets(5), eagerOffsets)

    val strTable: IncrementalTableVar[String] = IncrementalTableVar.allocate[String](it, offsets(6), eagerOffsets)

    val arrTable: IncrementalTableVar[IncrementalTableFixed[Ref]] = {
      IncrementalTableVar.allocateWith(it, offsets(7), eagerOffsets) {
        (it, offset, _) =>
          IncrementalTableFixed.allocate[Ref](it, offset)(using RefCodec, new DebugTableName("OneArr"))
      }(using new DebugTableName(DebugTableName.Arrays.tableName))
    }
    val objTable: IncrementalTableVar[OneObjTable] = IncrementalTableVar.allocateWith(it, offsets(8), eagerOffsets) {
      (it, offset, _) =>
        OneObjTable.allocate(it, offset, strTable, objectIndexBucketCount)
    }(using new DebugTableName(DebugTableName.Objects.tableName))
    val rootTable: IncrementalTableFixed[Root] = IncrementalTableFixed.allocate[Root](it, offsets(9))

    val roots = rootTable
      .readAll().iterator.map {
        case Root(id, ref) =>
          val rootId = strTable.readElem(id)
          rootId -> ref
      }.to(mutable.HashMap)

    new IncrementalEBAReader(
      it,
      intTable,
      longTable,
      bigIntTable,
      floatTable,
      doubleTable,
      bigDecTable,
      strTable,
      arrTable,
      objTable,
      rootTable,
      roots,
    )
  }

}

class IncrementalEBAReader(
  input: AutoCloseable,
  val intTable: IncrementalTableFixed[Int],
  val longTable: IncrementalTableFixed[Long],
  val bigIntTable: IncrementalTableVar[BigInt],
  val floatTable: IncrementalTableFixed[Float],
  val doubleTable: IncrementalTableFixed[Double],
  val bigDecTable: IncrementalTableVar[BigDecimal],
  val strTable: IncrementalTableVar[String],
  val arrTable: IncrementalTableVar[IncrementalTableFixed[Ref]],
  val objTable: IncrementalTableVar[OneObjTable],
  val rootTable: IncrementalTableFixed[Root],
  val roots: collection.Map[String, Ref],
) extends AutoCloseable {

  def getRoot(id: String): Option[Ref] = {
    roots.get(id)
  }

  def query(ref: Ref, path: String): IncrementalJValue = {
    query(ref, path.split(".").toList)
  }

  def query(ref: Ref, parts: List[String]): IncrementalJValue = {
    val res = queryRef(ref, parts)
    resolve(res)
  }

  def tryQuery(ref: Ref, path: String): Try[IncrementalJValue] = {
    Try(query(ref, path))
  }

  def tryQuery(jObj: IncrementalJValue.JObj, path: String): Try[IncrementalJValue] = {
    Try(query(jObj, path))
  }

  def query(jObj: IncrementalJValue.JObj, path: String): IncrementalJValue = {
    query(jObj, path.split(".").toList)
  }

  def query(jObj: IncrementalJValue.JObj, parts: List[String]): IncrementalJValue = {
    parts match {
      case Nil => jObj
      case currentQuery0 :: next0 =>
        val (currentQuery, next) = handleBracketsWithoutDot(currentQuery0, next0)
        val resolvedObj = jObj.obj.readObjectFieldRef(currentQuery)
        query(resolvedObj, next)
    }
  }

  def queryRef(ref: Ref, path: String): (Ref, Seq[String]) = {
    val parts = path.split(".").toList
    (queryRef(ref, parts), parts)
  }

  def queryRef(ref: Ref, parts: List[String]): Ref = {
    parts match {
      case Nil => ref
      case currentQuery0 :: next0 =>
        val (currentQuery, next) = handleBracketsWithoutDot(currentQuery0, next0)
        if (currentQuery.startsWith("[") && currentQuery.endsWith("]")) {
          val index = currentQuery.substring(1, currentQuery.length - 2)
          val iindex = index.toInt

          val resolvedArr = readArrayElementRef(ref, iindex)
          queryRef(resolvedArr, next)
        } else {
          val resolvedObj = readObjectFieldRef(ref, currentQuery)
          queryRef(resolvedObj, next)
        }
    }
  }

  def readObjectFieldRef(ref: Ref, field: String): Ref = {
    if (ref.kind == RefKind.TObj) {
      val obj = objTable.readElem(ref.ref)
      obj.readObjectFieldRef(field)
    } else {
      throw new IllegalStateException(
        s"Tried to find field $field in entity with id $ref which should be an object, but it was ${ref.kind}"
      )
    }
  }

  def readArrayElementRef(ref: Ref, iindex: Int): Ref = {
    if (ref.kind == RefKind.TArr) {
      val arr = arrTable.readElem(ref.ref)
      val i = if (iindex >= 0) iindex else arr.length + iindex // + decrements here because iindex is negative
      arr.readElem(i)
    } else {
      throw new IllegalStateException(
        s"Tried to find element $iindex in entity with id $ref which should be an array, but it was ${ref.kind}"
      )
    }
  }

  def resolve(ref: Ref): IncrementalJValue = {
    import IncrementalJValue.*
    ref.kind match {
      case RefKind.TNul =>
        JNul
      case RefKind.TBit =>
        JBit(ref.ref == 1)

      case RefKind.TByte =>
        JByte(ref.ref.toByte)
      case RefKind.TShort =>
        JShort(ref.ref.toShort)

      case RefKind.TInt =>
        JInt(intTable.readElem(ref.ref))
      case RefKind.TLng =>
        JLong(longTable.readElem(ref.ref))
      case RefKind.TBigInt =>
        JBigInt(bigIntTable.readElem(ref.ref))

      case RefKind.TFlt =>
        JFloat(floatTable.readElem(ref.ref))
      case RefKind.TDbl =>
        JDouble(doubleTable.readElem(ref.ref))
      case RefKind.TBigDec =>
        JBigDec(bigDecTable.readElem(ref.ref))

      case RefKind.TStr =>
        JString(strTable.readElem(ref.ref))

      case RefKind.TArr =>
        JArr(arrTable.readElem(ref.ref))
      case RefKind.TObj =>
        JObj(objTable.readElem(ref.ref))
      case RefKind.TRoot =>
        JRoot(rootTable.readElem(ref.ref))
    }
  }

  def resolveFull(ref: Ref): Json = {
    ref.kind match {
      case RefKind.TNul =>
        Json.Null
      case RefKind.TBit =>
        Json.fromBoolean(ref.ref == 1)

      case RefKind.TByte =>
        Json.fromInt(ref.ref)
      case RefKind.TShort =>
        Json.fromInt(ref.ref)

      case RefKind.TInt =>
        Json.fromInt(intTable.readElem(ref.ref))
      case RefKind.TLng =>
        Json.fromLong(longTable.readElem(ref.ref))
      case RefKind.TBigInt =>
        Json.fromBigInt(bigIntTable.readElem(ref.ref))

      case RefKind.TFlt =>
        Json.fromFloat(floatTable.readElem(ref.ref)).get
      case RefKind.TDbl =>
        Json.fromDouble(doubleTable.readElem(ref.ref)).get
      case RefKind.TBigDec =>
        Json.fromBigDecimal(bigDecTable.readElem(ref.ref))

      case RefKind.TStr =>
        Json.fromString(strTable.readElem(ref.ref))

      case RefKind.TArr =>
        val arr = arrTable.readElem(ref.ref)
        Json.fromValues(arr.readAll().map(resolveFull))
      case RefKind.TObj =>
        val obj = objTable.readElem(ref.ref)
        Json.fromFields(obj.readAll().map {
          case (k, v) =>
            (strTable.readElem(k), resolveFull(v))
        })
      case RefKind.TRoot =>
        resolveFull(rootTable.readElem(ref.ref).ref)
    }
  }

  override def close(): Unit = {
    input.close()
  }

  private def handleBracketsWithoutDot(currentQuery: String, next: List[String]): (String, List[String]) = {
    if (currentQuery.endsWith("]") && currentQuery.contains('[') && !currentQuery.startsWith("[")) {
      val index = currentQuery.substring(currentQuery.indexOf('['))
      (currentQuery.substring(0, currentQuery.indexOf('[')), index :: next)
    } else {
      (currentQuery, next)
    }
  }

}
