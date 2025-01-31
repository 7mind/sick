package izumi.sick.eba.writer

import izumi.sick.eba.writer.EBAEncoders.{DebugTableName, EBACodecFixedArray, EBACodecTable, EBACodecVar, IntToBytes}
import izumi.sick.eba.writer.util.computeSizesFromOffsets
import izumi.sick.eba.{EBATable, SICKSettings}
import izumi.sick.model.*
import izumi.sick.model.Ref.RefVal
import izumi.sick.thirdparty.akka.util.{ByteIterator, ByteString}
import izumi.sick.tools.CBFHash

import java.io.OutputStream
import java.math.{BigInteger, MathContext}
import java.nio.{ByteBuffer, ByteOrder}
import java.nio.charset.StandardCharsets
import scala.annotation.unused
import scala.collection.immutable.{ArraySeq, HashMap}
import scala.collection.mutable
import scala.reflect.{ClassTag, classTag}

@SuppressWarnings(Array("UnsafeTraversableMethods"))
class EBAEncoders(
  params: SICKWriterParameters
) {
  private val tableWriter: TableWriter = TableWriter(params.tableWriteStrategy)

  // tables of variable sized elements

  implicit def toBytesVarSizeTable[T](implicit varSizeCodec: EBACodecVar[T], debugTableName: DebugTableName[T]): EBACodecTable[T] = new EBACodecTable[T] {
    override def writeTable(stream: OutputStream, table: EBATable[T]): Long = {
      tableWriter.writeTable(stream, table, varSizeCodec)
    }

    override def readTable(it: ByteIterator): EBATable[T] = {
      val elemCount = it.getInt(using ByteOrder.BIG_ENDIAN)

      val offsets = ArraySeq.fill(elemCount + 1) {
        it.getInt(using ByteOrder.BIG_ENDIAN)
      }
      val sizes = computeSizesFromOffsets(offsets)
      val sizesSz = sizes.size

      val b = HashMap.newBuilder[RefVal, T]
      var i = 0
      while (i < sizesSz) {
        val sz = sizes(i)
        b += (RefVal(i) -> varSizeCodec.decode(it, sz))
        i += 1
      }
      val elems = b.result()
      new EBATable[T](debugTableName.tableName, elems)
    }
  }

  // tables of variable sized arrays of primitives

  implicit def toBytesFixedSizeArrayTable[T](implicit fixedArrayCodec: EBACodecFixedArray[T], debugTableName: DebugTableName[T]): EBACodecTable[T] =
    new EBACodecTable[T] {
      override def writeTable(stream: OutputStream, table: EBATable[T]): Long = {
        tableWriter.writeTable(stream, table, fixedArrayCodec)
      }

      override def readTable(it: ByteIterator): EBATable[T] = {
        val elemCount = it.getInt(using ByteOrder.BIG_ENDIAN)

        // ignore offsets when reading table fully in-memory
        it.drop(IntToBytes.blobSize * (elemCount + 1))

        val b = HashMap.newBuilder[RefVal, T]
        var i = 0
        while (i < elemCount) {
          b += (RefVal(i) -> fixedArrayCodec.decode(it))
          i += 1
        }
        val elems = b.result()
        new EBATable[T](debugTableName.tableName, elems)
      }
    }
}

object EBAEncoders {

  sealed abstract class EBAEncoder[T] {
    def encode(value: T): ByteString
  }

  sealed abstract class EBACodecFixed[T] extends EBAEncoder[T] {
    def blobSize: Int

    def decode(it: ByteIterator): T
  }

  sealed abstract class EBACodecVar[T] extends EBAEncoder[T] {
    def decode(it: ByteIterator, length: Int): T
  }

  sealed abstract class EBACodecTable[T] {
    def writeTable(stream: OutputStream, table: EBATable[T]): Long

    def readTable(it: ByteIterator): EBATable[T]
  }
  object EBACodecTable {
    def readTable[T](it: ByteIterator)(implicit codec: EBACodecTable[T]): EBATable[T] = {
      codec.readTable(it)
    }
  }

  sealed abstract class EBACodecFixedArray[T] extends EBAEncoder[T] {
    def elementSize: Int

    def decode(it: ByteIterator): T
  }

  final class DebugTableName[@unused T](val tableName: String) extends AnyVal
  object DebugTableName {
    implicit final def Strings: DebugTableName[String] = new DebugTableName("Strings")

    implicit final def Integers: DebugTableName[Int] = new DebugTableName("Integers")
    implicit final def Longs: DebugTableName[Long] = new DebugTableName("Longs")
    implicit final def Bigints: DebugTableName[BigInt] = new DebugTableName("Bigints")

    implicit final def Floats: DebugTableName[Float] = new DebugTableName("Floats")
    implicit final def Doubles: DebugTableName[Double] = new DebugTableName("Doubles")
    implicit final def BigDecs: DebugTableName[BigDecimal] = new DebugTableName("BigDecs")

    implicit final def Arrays: DebugTableName[Arr] = new DebugTableName("Arrays")
    implicit final def Objects: DebugTableName[Obj] = new DebugTableName("Objects")
    implicit final def Roots: DebugTableName[Root] = new DebugTableName("Roots")
  }

  // primitive fixed types

  implicit val ByteToBytes: EBACodecFixed[Byte] = new EBACodecFixed[Byte] {
    override def blobSize: Int = 1

    override def encode(value: Byte): ByteString = {
      ByteString(value)
    }

    override def decode(it: ByteIterator): Byte = {
      it.next()
    }
  }

  implicit val ShortToBytes: EBACodecFixed[Short] = new EBACodecFixed[Short] {
    override def blobSize: Int = java.lang.Short.BYTES

    override def encode(value: Short): ByteString = {
      fromByteBuffer(blobSize)(_.putShort(value))
    }

    override def decode(it: ByteIterator): Short = {
      it.getShort(using ByteOrder.BIG_ENDIAN)
    }
  }

  // unsigned Short
  implicit val CharToBytes: EBACodecFixed[Char] = new EBACodecFixed[Char] {
    override def blobSize: Int = java.lang.Character.BYTES

    override def encode(value: Char): ByteString = {
      fromByteBuffer(blobSize)(_.putChar(value))
    }

    override def decode(it: ByteIterator): Char = {
      it.getShort(using ByteOrder.BIG_ENDIAN).toChar
    }
  }

  implicit val IntToBytes: EBACodecFixed[Int] = new EBACodecFixed[Int] {
    override def blobSize: Int = java.lang.Integer.BYTES

    override def encode(value: Int): ByteString = {
      fromByteBuffer(blobSize)(_.putInt(value))
    }

    override def decode(it: ByteIterator): Int = {
      it.getInt(using ByteOrder.BIG_ENDIAN)
    }
  }

  @inline implicit final def RefValToBytes: EBACodecFixed[RefVal] = IntToBytes.asInstanceOf[EBACodecFixed[RefVal]]

  implicit val LongToBytes: EBACodecFixed[Long] = new EBACodecFixed[Long] {
    override def blobSize: Int = java.lang.Long.BYTES

    override def encode(value: Long): ByteString = {
      fromByteBuffer(blobSize)(_.putLong(value))
    }

    override def decode(it: ByteIterator): Long = {
      it.getLong(using ByteOrder.BIG_ENDIAN)
    }
  }

  implicit val FloatToBytes: EBACodecFixed[Float] = new EBACodecFixed[Float] {
    override def blobSize: Int = java.lang.Float.BYTES

    override def encode(value: Float): ByteString = {
      fromByteBuffer(blobSize)(_.putFloat(value))
    }

    override def decode(it: ByteIterator): Float = {
      it.getFloat(using ByteOrder.BIG_ENDIAN)
    }
  }

  implicit val DoubleToBytes: EBACodecFixed[Double] = new EBACodecFixed[Double] {
    override def blobSize: Int = java.lang.Double.BYTES

    override def encode(value: Double): ByteString = {
      fromByteBuffer(blobSize)(_.putDouble(value))
    }

    override def decode(it: ByteIterator): Double = {
      it.getDouble(using ByteOrder.BIG_ENDIAN)
    }
  }

  implicit val RefKindToBytes: EBACodecFixed[RefKind] = new EBACodecFixed[RefKind] {
    override def blobSize: Int = 1

    override def encode(value: RefKind): ByteString = {
      ByteToBytes.encode(value.index)
    }

    override def decode(it: ByteIterator): RefKind = {
      RefKind.fromIndex(it.next())
    }
  }

  implicit val RefToBytes: EBACodecFixed[Ref] = new EBACodecFixed[Ref] {
    override def blobSize: Int = 1 + Integer.BYTES

    override def encode(value: Ref): ByteString = {
      val out = RefKindToBytes.encode(value.kind) ++ RefValToBytes.encode(value.ref)
      assert(out.size == blobSize)
      out
    }

    override def decode(it: ByteIterator): Ref = {
      val kind = RefKindToBytes.decode(it)
      val refVal = RefValToBytes.decode(it)
      Ref(kind, Ref.RefVal(refVal))
    }
  }

  implicit val ObjectEntryToBytes: EBACodecFixed[(RefVal, Ref)] = new EBACodecFixed[(RefVal, Ref)] {
    override def blobSize: Int = Integer.BYTES * 2 + 1

    override def encode(value: (RefVal, Ref)): ByteString = {
      val out = RefValToBytes.encode(value._1) ++ RefToBytes.encode(value._2)
      assert(out.size == blobSize)
      out
    }

    override def decode(it: ByteIterator): (RefVal, Ref) = {
      val refVal = RefValToBytes.decode(it)
      val ref = RefToBytes.decode(it)
      (refVal, ref)
    }
  }

  implicit val RootToBytes: EBACodecFixed[Root] = new EBACodecFixed[Root] {
    override def blobSize: Int = ObjectEntryToBytes.blobSize

    override def encode(value: Root): ByteString = {
      ObjectEntryToBytes.encode(value.id, value.ref)
    }

    override def decode(it: ByteIterator): Root = {
      val tuple = ObjectEntryToBytes.decode(it)
      Root(tuple._1, tuple._2)
    }
  }

  // primitive variable-size types

  implicit val StringToBytes: EBACodecVar[String] = new EBACodecVar[String] {
    override def encode(value: String): ByteString = {
      ByteString.fromArrayUnsafe(value.getBytes(StandardCharsets.UTF_8))
    }

    override def decode(it: ByteIterator, length: Int): String = {
      val bytes = new Array[Byte](length)
      it.getBytes(bytes, 0, length)
      new String(bytes, 0, length, StandardCharsets.UTF_8)
    }
  }

  implicit val BigIntToBytes: EBACodecVar[BigInt] = new EBACodecVar[BigInt] {
    override def encode(value: BigInt): ByteString = {
      ByteString.fromArrayUnsafe(value.toByteArray)
    }

    override def decode(it: ByteIterator, length: Int): BigInt = {
      val bytes = new Array[Byte](length)
      it.getBytes(bytes, 0, length)
      BigInt(bytes)
    }
  }

  implicit val BigDecimalToBytes: EBACodecVar[BigDecimal] = new EBACodecVar[BigDecimal] {
    override def encode(value: BigDecimal): ByteString = {
      IntToBytes.encode(value.underlying().signum())
        ++ IntToBytes.encode(value.underlying().precision())
        ++ IntToBytes.encode(value.underlying().scale())
        ++ ByteString(value.underlying().unscaledValue().toByteArray)
    }

    override def decode(it: ByteIterator, length: Int): BigDecimal = {
      val signum = IntToBytes.decode(it)
      val precision = IntToBytes.decode(it)
      val scale = IntToBytes.decode(it)
      val unscaledLength = length - (3 * IntToBytes.blobSize)
      val unscaledBytes = new Array[Byte](unscaledLength)
      it.getBytes(unscaledBytes, 0, unscaledLength)

      val unscaled = new BigInteger(unscaledBytes)
      val res = BigDecimal(BigInt(unscaled), scale, new MathContext(precision)) * signum

      assert(res.signum == signum)
      assert(res.precision == precision)
      assert(res.scale == scale)
      assert(res.underlying().unscaledValue().equals(unscaled))

      res
    }
  }

  // arrays of primitive types

  implicit def toBytesFixedSizeSeq[T: ClassTag](implicit codecFixed: EBACodecFixed[T]): EBACodecFixedArray[Seq[T]] = new EBACodecFixedArray[Seq[T]] {
    override def elementSize: Int = codecFixed.blobSize

    override def encode(value: Seq[T]): ByteString = {
      val b = ByteString.newBuilder
      b.putInt(value.length)(using ByteOrder.BIG_ENDIAN)
      value.foreach(b ++= codecFixed.encode(_))
      b.result()
    }

    override def decode(it: ByteIterator): Seq[T] = {
      val count = it.getInt(using ByteOrder.BIG_ENDIAN)
      val elems = new Array[T](count)
      var i = 0
      while (i < count) {
        elems(i) = codecFixed.decode(it)
        i += 1
      }
      ArraySeq.unsafeWrapArray(elems)
    }
  }

  implicit val ArrToBytes: EBACodecFixedArray[Arr] = new EBACodecFixedArray[Arr] {
    private val underlying: EBACodecFixedArray[Seq[Ref]] = toBytesFixedSizeSeq(using classTag, RefToBytes)

    override def elementSize: Int = implicitly[EBACodecFixed[Ref]].blobSize

    override def encode(value: Arr): ByteString = {
      underlying.encode(value.values)
    }

    override def decode(it: ByteIterator): Arr = {
      Arr(Vector.from(underlying.decode(it)))
    }
  }

  // tables of primitive types

  implicit def toBytesFixedSizeTable[T](implicit elemCodec: EBACodecFixed[T], debugTableName: DebugTableName[T]): EBACodecTable[T] = new EBACodecTable[T] {

    override def writeTable(stream: OutputStream, table: EBATable[T]): Long = {
      val elemCount = table.size
      stream.write(IntToBytes.encode(elemCount).toArrayUnsafe())
      var totalTableSize: Long = IntToBytes.blobSize.toLong
      table.forEach {
        elem =>
          stream.write(elemCodec.encode(elem).toArrayUnsafe())
          totalTableSize += elemCodec.blobSize
      }
      totalTableSize
    }

    override def readTable(it: ByteIterator): EBATable[T] = {
      val count = IntToBytes.decode(it)
      var i = 0
      val b = HashMap.newBuilder[RefVal, T]
      while (i < count) {
        val elem = elemCodec.decode(it)
        b.addOne(RefVal(i) -> elem)
        i += 1
      }
      new EBATable(debugTableName.tableName, b.result())
    }

//    override def write(stream: FileOutputStream, table: EBATable[T], params: SICKWriterParameters): Long = {
//      val before = stream.getChannel.position()
//      stream.write(table.size.bytes.toArray)
//
//      table.forEach {
//        s =>
//          stream.write(s.bytes.toArray)
//
//      }
//      val after = stream.getChannel.position()
//      after - before
//    }
  }

  // objects

  def ObjToBytes(strings: EBATable[String], packSettings: SICKSettings): EBACodecFixedArray[Obj] = new EBACodecFixedArray[Obj] {
    override def elementSize: Int = ObjectEntryToBytes.blobSize

    private val ObjectEntrySeqCodec: EBACodecFixedArray[Seq[(RefVal, Ref)]] = toBytesFixedSizeSeq[(RefVal, Ref)](using classTag, ObjectEntryToBytes)

    private[this] final val noIndexMarker = 65535
    assert(noIndexMarker == Char.MaxValue)

    private[this] final val maxIndex = 65534
    assert(maxIndex == noIndexMarker - 1)

    private[this] final val range = 0xFFFFFFFFL + 1 // uint max + 1
    assert(range == Math.abs(Integer.MIN_VALUE.toLong) + Integer.MAX_VALUE.toLong + 1)

    @SuppressWarnings(Array("UnnecessaryConversion"))
    override def encode(value: Obj): ByteString = {
      val bucketCount: Short = packSettings.objectIndexBucketCount
      val indexingThreshold: Short = packSettings.minObjectKeysBeforeIndexing

      val objEntriesSortedByCBFHash: ArraySeq[((RefVal, Ref), (Long, Int))] = {
        val bucketSize = range / bucketCount
        value.values
          .to(ArraySeq)
          .map {
            case (k, v) =>
              val kval = strings(k)
              val hash = CBFHash.compute(kval)
              val bucket = (hash / bucketSize).toInt
              ((k, v), (hash, bucket))
          }.sortBy(_._2._1)
      }

      if (objEntriesSortedByCBFHash.size >= maxIndex) {
        throw new RuntimeException(s"Too many keys in object, object can't contain more than $noIndexMarker")
      }

      val index: mutable.ArrayBuffer[Int] = {
        if (objEntriesSortedByCBFHash.size <= indexingThreshold) {
          mutable.ArrayBuffer(noIndexMarker) // if the object is small, we put an array with one marker element
        } else {
          val index = mutable.ArrayBuffer.fill(bucketCount.toInt)(maxIndex)
          var idx = 0
          objEntriesSortedByCBFHash.foreach {
            case ((_, _), (_, bucket)) =>
              val currentVal = index(bucket)
              if (currentVal == maxIndex) {
                index(bucket) = idx
              }
              idx += 1
          }
          index
        }
      }
      assert(index.length == 1 || index.length == bucketCount)

      if (index.length == bucketCount) {
        var last = objEntriesSortedByCBFHash.size
        var i = bucketCount - 1
        while (i >= 0) {
          if (index(i) == maxIndex) {
            index(i) = last
          } else {
            last = index(i)
          }
          i = i - 1
        }
      } else if (index.length > 1) {
        throw new IllegalStateException(s"Wrong index size: ${index.length}, expected $bucketCount or 1")
      }

      val indexHeader = index.foldLeft(ByteString.empty) {
        (b, i) =>
          assert(i >= 0 && i <= noIndexMarker)
          b ++ CharToBytes.encode(i.toChar)
      }
      assert(indexHeader.size == Character.BYTES * bucketCount || indexHeader.size == Character.BYTES * 1)

      val objectEntries = objEntriesSortedByCBFHash.map(_._1)
      val objectEntryArray = ObjectEntrySeqCodec.encode(objectEntries)

      indexHeader ++ objectEntryArray
    }

    override def decode(it: ByteIterator): Obj = {
      val indexMarker = CharToBytes.decode(it)

      val entries = if (indexMarker == noIndexMarker) {
        ObjectEntrySeqCodec.decode(it)
      } else {
        val bucketCount: Short = packSettings.objectIndexBucketCount

        // we don't need the indexHeader when reading the Object whole, so just skip it
        it.drop(Character.BYTES * (bucketCount - 1))

        ObjectEntrySeqCodec.decode(it)
      }

      Obj(Map.from(entries))
    }
  }

  private def fromByteBuffer(blobSize: Int)(b: ByteBuffer => Any): ByteString = {
    val bb = ByteBuffer.allocate(blobSize)
    b(bb)
    ByteString.fromArrayUnsafe(bb.array())
  }

}
