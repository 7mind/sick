package izumi.sick.indexes

import io.circe.Json
import izumi.sick.model
import izumi.sick.model._
import izumi.sick.tables.Bijection

object RWIndex {
  def apply(): RWIndex = {
    val strings = Bijection[String]("Strings")

//    val bytes = Bijection[Byte]("Bytes")
//    val shorts = Bijection[Short]("Shorts")
    val ints = Bijection[Int]("Integers")
    val longs = Bijection[Long]("Longs")
    val bigints = Bijection[BigInt]("Bigints")

    val floats = Bijection[Float]("Floats")
    val doubles = Bijection[Double]("Doubles")
    val bigDecimals = Bijection[BigDecimal]("BigDecs")

    val arrs = Bijection[Arr]("Arrays")
    val objs = Bijection[Obj]("Objects")
    val roots = Bijection[Root]("Roots")
    new RWIndex(
      strings,
//      bytes,
//      shorts,
      ints,
      longs,
      bigints,
      floats,
      doubles,
      bigDecimals,
      arrs,
      objs,
      roots,
    )
  }
}
class RWIndex private (
  strings: Bijection[String],
//  bytes: Bijection[Byte],
//  shorts: Bijection[Short],
  ints: Bijection[Int],
  longs: Bijection[Long],
  bigints: Bijection[BigInt],
  floats: Bijection[Float],
  doubles: Bijection[Double],
  bigDecimals: Bijection[BigDecimal],
  arrs: Bijection[Arr],
  objs: Bijection[Obj],
  roots: Bijection[Root],
) {
  def findRoot(str: String): Option[Root] = {
    println(strings.revGet(str))
    strings.revGet(str).flatMap(si => roots.all().find(_._2._1.id == si).map(_._2._1))
  }

  def freeze(): ROIndex = {
//    println(shorts.all().map(_._2._1).toList.sorted.mkString(","))
    new ROIndex(
//      bytes.freeze(),
//      shorts.freeze(),
      ints.freeze(),
      longs.freeze(),
      bigints.freeze(),
      floats.freeze(),
      doubles.freeze(),
      bigDecimals.freeze(),
      strings.freeze(),
      arrs.freeze(),
      objs.freeze(),
      roots.freeze(),
    )
  }

  override def toString: String = {
    Seq(strings, ints, longs, bigints, floats, doubles, bigDecimals, arrs, objs, roots).filterNot(_.isEmpty).mkString("\n\n")
  }

  def reconstruct(ref: Ref): Json = {
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
        Json.fromInt(ints(ref.ref))
      case RefKind.TLng =>
        Json.fromLong(longs(ref.ref))
      case RefKind.TBigInt =>
        Json.fromBigInt(bigints(ref.ref))

      case RefKind.TFlt =>
        Json.fromFloat(floats(ref.ref)).get
      case RefKind.TDbl =>
        Json.fromDouble(doubles(ref.ref)).get
      case RefKind.TBigDec =>
        Json.fromBigDecimal(bigDecimals(ref.ref))

      case RefKind.TStr =>
        Json.fromString(strings(ref.ref))

      case RefKind.TArr =>
        val a = arrs(ref.ref)
        Json.fromValues(a.values.map(reconstruct))
      case RefKind.TObj =>
        val o = objs(ref.ref)
        Json.fromFields(o.values.map {
          case (k, v) =>
            (strings(k), reconstruct(v))
        })
      case RefKind.TRoot =>
        // this shouldn't actually happen
        reconstruct(roots(ref.ref).ref)
    }
  }

  def rebuild(): RWIndex = {
    def rebuildSimpleTable[V](table: Bijection[V], tpe: RefKind) = {
      val data = table.all().toSeq.sortBy(_._2._2)(Ordering.Int.reverse).zipWithIndex.map {
        case ((originalRef, (target, freq)), newRef) =>
          (originalRef, (newRef, target, freq))
      }
      val updated = Bijection.fromMonotonic(table.name, data.map(_._2))
      (updated, data.map { case (origRef, (newRef, _, _)) => Ref(tpe, origRef) -> Ref(tpe, newRef) }.toMap)
    }

    val (newInts, intmapMap) = rebuildSimpleTable(ints, RefKind.TInt)
    val (newLongs, longMap) = rebuildSimpleTable(longs, RefKind.TLng)
    val (newBigints, bigintMap) = rebuildSimpleTable(bigints, RefKind.TBigInt)
    val (newFloats, floatMap) = rebuildSimpleTable(floats, RefKind.TFlt)
    val (newDoubles, doubleMap) = rebuildSimpleTable(doubles, RefKind.TDbl)
    val (newBigdecs, bigdecMap) = rebuildSimpleTable(bigDecimals, RefKind.TBigDec)
    val (newStrs, stringMap) = rebuildSimpleTable(strings, RefKind.TStr)
    val (newArrs, arrMap) = rebuildSimpleTable(arrs, RefKind.TArr)
    val (newObjs, objMap) = rebuildSimpleTable(objs, RefKind.TObj)
    val (newRoots, rootMap) = rebuildSimpleTable(roots, RefKind.TRoot)

    val fullMap = Seq(intmapMap, longMap, bigintMap, floatMap, doubleMap, bigdecMap, stringMap, arrMap, objMap, rootMap).flatten.toMap

    new RWIndex(
      newStrs,
      newInts,
      newLongs,
      newBigints,
      newFloats,
      newDoubles,
      newBigdecs,
      newArrs.remap(fullMap),
      newObjs.remap(fullMap),
      newRoots.remap(fullMap),
    )

  }

  def append(id: String, j: Json): Ref = {
    val idRef = addString(id)
    val root = traverse(j)
    addRoot(Root(idRef.ref, root))
    root
  }

  private def traverse(j: Json): Ref = {
    j.fold(
      Ref(RefKind.TNul, 0),
      b =>
        Ref(
          RefKind.TBit,
          if (b) { 1 }
          else { 0 },
        ),
      n => {
        n.toBigDecimal match {
          case Some(value) if value.isWhole && value.isValidInt =>
            val intValue = value.toIntExact
            if (intValue <= java.lang.Byte.MAX_VALUE && intValue >= java.lang.Byte.MIN_VALUE) {
              Ref(RefKind.TByte, intValue)
            } else if (intValue <= java.lang.Short.MAX_VALUE && intValue >= java.lang.Short.MIN_VALUE) {
              Ref(RefKind.TShort, intValue)
            } else if (intValue <= java.lang.Integer.MAX_VALUE && intValue >= java.lang.Integer.MIN_VALUE) {
              addInt(intValue)
            } else {
              throw new IllegalStateException(s"Cannot decode number $n")
            }
          case Some(value) if value.isWhole && value.isValidLong =>
            addLong(value.toLongExact)
          case Some(value) if value.isWhole =>
            addBigInt(value.toBigIntExact.getOrElse(throw new IllegalStateException(s"Cannot decode BigInt $n")))
          case Some(value) if value.isDecimalFloat =>
            addFloat(value.floatValue)
          case Some(value) if value.isDecimalDouble =>
            addDouble(value.doubleValue)
          case Some(value) =>
            addBigDec(value)
          case None =>
            throw new IllegalStateException(s"Cannot decode number $n")
        }
      },
      s => addString(s),
      arr =>
        addArr(
          Arr(
            arr.map(traverse)
          )
        ),
      obj =>
        addObj(
          Obj(
            obj.toMap.toVector.map {
              case (k, v) =>
                (addString(k).ref, traverse(v))
            }
          )
        ),
    )
  }

  private def addString(s: String): Ref = model.Ref(RefKind.TStr, strings.add(s))
  private def addInt(s: Int): Ref = model.Ref(RefKind.TInt, ints.add(s))
  private def addLong(s: Long): Ref = model.Ref(RefKind.TLng, longs.add(s))
  private def addBigInt(s: BigInt): Ref = model.Ref(RefKind.TBigInt, bigints.add(s))
  private def addFloat(s: Float): Ref = model.Ref(RefKind.TFlt, floats.add(s))
  private def addDouble(s: Double): Ref = model.Ref(RefKind.TDbl, doubles.add(s))
  private def addBigDec(s: BigDecimal): Ref = model.Ref(RefKind.TBigDec, bigDecimals.add(s))
  private def addArr(s: Arr): Ref = model.Ref(RefKind.TArr, arrs.add(s))
  private def addObj(s: Obj): Ref = model.Ref(RefKind.TObj, objs.add(s))
  private def addRoot(s: Root): Ref = {
    roots.revGet(s) match {
      case Some(value) =>
        throw new IllegalStateException(s"Root $s already exists with ref $value")
      case None =>
        model.Ref(RefKind.TRoot, roots.add(s))
    }
  }

}
