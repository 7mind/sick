package izumi.sick.eba.writer.codecs

import izumi.sick.eba.writer.codecs.EBACodecs.{EBACodecFixedArray, EBACodecVar, EBAEncoderTable}
import izumi.sick.eba.writer.codecs.util.computeSizesFromOffsets
import izumi.sick.eba.{EBATable, SICKSettings}
import izumi.sick.model.*
import izumi.sick.model.Ref.RefVal
import izumi.sick.thirdparty.akka.util.ByteString.ByteString1C
import izumi.sick.thirdparty.akka.util.{ByteString, ByteStringBuilder}
import izumi.sick.tools.CBFHash

import java.io.{DataInput, OutputStream}
import java.math.{BigInteger, MathContext}
import java.nio.charset.StandardCharsets
import java.nio.ByteOrder
import scala.annotation.unused
import scala.collection.immutable.ArraySeq
import scala.collection.mutable
import scala.reflect.{ClassTag, classTag}

@SuppressWarnings(Array("UnsafeTraversableMethods"))
class EBACodecs(
  params: SICKWriterParameters
) {
  private val tableWriter: TableWriter = TableWriter(params.tableWriteStrategy)

  implicit def VarSizeTableEncoder[T](implicit varSizeCodec: EBACodecVar[T]): EBAEncoderTable[T] = new EBAEncoderTable[T] {
    override def writeTable(stream: OutputStream, table: EBATable[T]): Long = {
      tableWriter.writeTable(stream, table, varSizeCodec)
    }
  }

  implicit def FixedSizeArrayTableEncoder[T](implicit fixedArrayCodec: EBACodecFixedArray[T]): EBAEncoderTable[T] =
    new EBAEncoderTable[T] {
      override def writeTable(stream: OutputStream, table: EBATable[T]): Long = {
        tableWriter.writeTable(stream, table, fixedArrayCodec)
      }
    }
}

object EBACodecs {

  abstract class EBAEncoder[T] {
    def encodeSlow(value: T): ByteString = { val b = ByteString.newBuilder; encodeTo(value, b); b.result() }
    def encodeTo(value: T, b: ByteStringBuilder): Int
    def computeSize(value: T): Int
  }

  abstract class EBACodecFixed[T] extends EBAEncoder[T] {
    def blobSize: Int

    def decode(it: DataInput): T

    override final def computeSize(value: T): Int = blobSize
  }

  abstract class EBACodecVar[T] extends EBAEncoder[T] {
    def decode(it: DataInput, length: Int): T
  }

  abstract class EBADecoderTable[T] {
    def readTable(it: DataInput): EBATable[T]
  }
  object EBADecoderTable {
    def readTable[T](it: DataInput)(implicit codec: EBADecoderTable[T]): EBATable[T] = {
      codec.readTable(it)
    }
  }

  trait EBAEncoderTable[T] {
    def writeTable(stream: OutputStream, table: EBATable[T]): Long
  }

  abstract class EBACodecFixedArray[T] extends EBAEncoder[T] {
    def elementSize: Int

    def decode(it: DataInput): T
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

  implicit object ByteCodec extends EBACodecFixed[Byte] {
    override final val blobSize = 1

    override def encodeSlow(value: Byte): ByteString = {
      ByteString(value)
    }

    override def encodeTo(value: Byte, b: ByteStringBuilder): Int = {
      b.putByte(value)
      blobSize
    }

    override def decode(it: DataInput): Byte = {
      it.readByte()
    }
  }

  implicit object ShortCodec extends EBACodecFixed[Short] {
    override final val blobSize = java.lang.Short.BYTES

    override def encodeTo(value: Short, b: ByteStringBuilder): Int = {
      b.putShort(value.toInt)(ByteOrder.BIG_ENDIAN)
      blobSize
    }

    override def decode(it: DataInput): Short = {
      it.readShort()
    }
  }

  // unsigned Short
  implicit object CharCodec extends EBACodecFixed[Char] {
    override final val blobSize = java.lang.Character.BYTES

    override def encodeTo(value: Char, b: ByteStringBuilder): Int = {
      b.putShort(value.toInt)(ByteOrder.BIG_ENDIAN)
      blobSize
    }

    override def decode(it: DataInput): Char = {
      it.readChar()
    }
  }

  implicit object IntCodec extends EBACodecFixed[Int] {
    override final val blobSize = java.lang.Integer.BYTES

    override def encodeSlow(value: Int): ByteString = {
      val target = new Array[Byte](4)
      target(0) = (value >>> 24).toByte
      target(1) = (value >>> 16).toByte
      target(2) = (value >>> 8).toByte
      target(3) = (value >>> 0).toByte
      ByteString1C(target)
    }

    override def encodeTo(value: Int, b: ByteStringBuilder): Int = {
      b.putInt(value)(ByteOrder.BIG_ENDIAN)
      blobSize
    }

    override def decode(it: DataInput): Int = {
      it.readInt()
    }
  }

  @inline implicit final def RefValCodec: EBACodecFixed[RefVal] = IntCodec.asInstanceOf[EBACodecFixed[RefVal]]

  implicit object LongCodec extends EBACodecFixed[Long] {
    override final val blobSize = java.lang.Long.BYTES

    override def encodeSlow(value: Long): ByteString = {
      val target = new Array[Byte](8)
      target(0) = (value >>> 56).toByte
      target(1) = (value >>> 48).toByte
      target(2) = (value >>> 40).toByte
      target(3) = (value >>> 32).toByte
      target(4) = (value >>> 24).toByte
      target(5) = (value >>> 16).toByte
      target(6) = (value >>> 8).toByte
      target(7) = (value >>> 0).toByte
      ByteString.ByteString1C(target)
    }

    override def encodeTo(value: Long, b: ByteStringBuilder): Int = {
      b.putLong(value)(ByteOrder.BIG_ENDIAN)
      blobSize
    }

    override def decode(it: DataInput): Long = {
      it.readLong()
    }
  }

  implicit object FloatCodec extends EBACodecFixed[Float] {
    override final val blobSize = java.lang.Float.BYTES

    override def encodeSlow(value: Float): ByteString = {
      IntCodec.encodeSlow(java.lang.Float.floatToRawIntBits(value))
    }

    override def encodeTo(value: Float, b: ByteStringBuilder): Int = {
      b.putFloat(value)(ByteOrder.BIG_ENDIAN)
      blobSize
    }

    override def decode(it: DataInput): Float = {
      it.readFloat()
    }
  }

  implicit object DoubleCodec extends EBACodecFixed[Double] {
    override final val blobSize = java.lang.Double.BYTES

    override def encodeSlow(value: Double): ByteString = {
      LongCodec.encodeSlow(java.lang.Double.doubleToRawLongBits(value))
    }

    override def encodeTo(value: Double, b: ByteStringBuilder): Int = {
      b.putDouble(value)(ByteOrder.BIG_ENDIAN)
      blobSize
    }

    override def decode(it: DataInput): Double = {
      it.readDouble()
    }
  }

  implicit object RefKindCodec extends EBACodecFixed[RefKind] {
    override final val blobSize = 1

    override def encodeSlow(value: RefKind): ByteString = {
      ByteCodec.encodeSlow(value.index)
    }

    override def encodeTo(value: RefKind, b: ByteStringBuilder): Int = {
      ByteCodec.encodeTo(value.index, b)
      blobSize
    }

    override def decode(it: DataInput): RefKind = {
      RefKind.fromIndex(it.readByte())
    }
  }

  implicit object RefCodec extends EBACodecFixed[Ref] {
    override final val blobSize = 1 + Integer.BYTES

    override def encodeSlow(value: Ref): ByteString = {
      val out = RefKindCodec.encodeSlow(value.kind) ++ RefValCodec.encodeSlow(value.ref)
      assert(out.size == blobSize)
      out
    }

    override def encodeTo(value: Ref, b: ByteStringBuilder): Int = {
      RefKindCodec.encodeTo(value.kind, b)
      RefValCodec.encodeTo(value.ref, b)
      blobSize
    }

    override def decode(it: DataInput): Ref = {
      val kind = RefKindCodec.decode(it)
      val refVal = RefValCodec.decode(it)
      Ref(kind, Ref.RefVal(refVal))
    }
  }

  implicit object ObjectEntryCodec extends EBACodecFixed[(RefVal, Ref)] {
    override final val blobSize = Integer.BYTES * 2 + 1

    override def encodeSlow(value: (RefVal, Ref)): ByteString = {
      val out = RefValCodec.encodeSlow(value._1) ++ RefCodec.encodeSlow(value._2)
      assert(out.size == blobSize)
      out
    }

    override def encodeTo(value: (RefVal, Ref), b: ByteStringBuilder): Int = {
      RefValCodec.encodeTo(value._1, b)
      RefCodec.encodeTo(value._2, b)
      blobSize
    }

    override def decode(it: DataInput): (RefVal, Ref) = {
      val refVal = RefValCodec.decode(it)
      val ref = RefCodec.decode(it)
      (refVal, ref)
    }
  }

  implicit object RootCodec extends EBACodecFixed[Root] {
    override final val blobSize = ObjectEntryCodec.blobSize

    override def encodeSlow(value: Root): ByteString = {
      ObjectEntryCodec.encodeSlow((value.id, value.ref))
    }

    override def encodeTo(value: Root, b: ByteStringBuilder): Int = {
      ObjectEntryCodec.encodeTo((value.id, value.ref), b)
      blobSize
    }

    override def decode(it: DataInput): Root = {
      val tuple = ObjectEntryCodec.decode(it)
      Root(tuple._1, tuple._2)
    }
  }

  // primitive variable-size types

  implicit object StringCodec extends EBACodecVar[String] {
    override def encodeSlow(value: String): ByteString = {
      ByteString.ByteString1C(value.getBytes(StandardCharsets.UTF_8))
    }

    override def encodeTo(value: String, b: ByteStringBuilder): Int = {
      val bytes = value.getBytes(StandardCharsets.UTF_8)
      val sz = bytes.length
      b.putBytes(bytes)
      sz
    }

    override def computeSize(value: String): Int = {
      value.getBytes(StandardCharsets.UTF_8).length
    }

    override def decode(it: DataInput, length: Int): String = {
      val bytes = new Array[Byte](length)
      it.readFully(bytes, 0, length)
      new String(bytes, 0, length, StandardCharsets.UTF_8)
    }
  }

  implicit object BigIntCodec extends EBACodecVar[BigInt] {
    override def encodeSlow(value: BigInt): ByteString = {
      ByteString.ByteString1C(value.toByteArray)
    }

    override def encodeTo(value: BigInt, b: ByteStringBuilder): Int = {
      val bytes = value.toByteArray
      val sz = bytes.length
      b.putBytes(bytes)
      sz
    }

    override def computeSize(value: BigInt): Int = {
      value.bigInteger.bitLength() / 8 + 1
    }

    override def decode(it: DataInput, length: Int): BigInt = {
      val bytes = new Array[Byte](length)
      it.readFully(bytes, 0, length)
      BigInt(bytes)
    }
  }

  implicit object BigDecimalCodec extends EBACodecVar[BigDecimal] {
    override def encodeTo(value: BigDecimal, b: ByteStringBuilder): Int = {
      IntCodec.encodeTo(value.underlying().signum(), b)
      IntCodec.encodeTo(value.underlying().precision(), b)
      IntCodec.encodeTo(value.underlying().scale(), b)
      val bytes = value.underlying().unscaledValue().toByteArray
      val sz = bytes.length
      b.putBytes(bytes)
      IntCodec.blobSize * 3 + sz
    }

    override def computeSize(value: BigDecimal): Int = {
      IntCodec.blobSize * 3 + (value.underlying().unscaledValue().bitLength() / 8 + 1)
    }

    override def decode(it: DataInput, length: Int): BigDecimal = {
      val signum = IntCodec.decode(it)
      val precision = IntCodec.decode(it)
      val scale = IntCodec.decode(it)
      val unscaledLength = length - (3 * IntCodec.blobSize)
      val unscaledBytes = new Array[Byte](unscaledLength)
      it.readFully(unscaledBytes, 0, unscaledLength)

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

  @inline implicit def FixedSizeSeqCodec[T: ClassTag: EBACodecFixed]: EBACodecFixedArray[Seq[T]] = new FixedSizeSeqCodec[T]

  final class FixedSizeSeqCodec[T: ClassTag](implicit codecFixed: EBACodecFixed[T]) extends EBACodecFixedArray[Seq[T]] {
    override def elementSize: Int = codecFixed.blobSize

    override def encodeTo(value: Seq[T], b: ByteStringBuilder): Int = {
      val sz = value.length
      b.putInt(sz)(using ByteOrder.BIG_ENDIAN)
      value.foreach(codecFixed.encodeTo(_, b))
      elementSize * sz
    }

    override def computeSize(value: Seq[T]): Int = {
      value.size * elementSize
    }

    override def decode(it: DataInput): Seq[T] = {
      val count = IntCodec.decode(it)
      val elems = new Array[T](count)
      var i = 0
      while (i < count) {
        elems(i) = codecFixed.decode(it)
        i += 1
      }
      ArraySeq.unsafeWrapArray(elems)
    }
  }

  implicit object ArrCodec extends EBACodecFixedArray[Arr] {
    private val underlying: EBACodecFixedArray[Seq[Ref]] = FixedSizeSeqCodec(using classTag, RefCodec)

    override def elementSize: Int = RefCodec.blobSize

    override def encodeSlow(value: Arr): ByteString = {
      underlying.encodeSlow(value.values)
    }

    override def encodeTo(value: Arr, b: ByteStringBuilder): Int = {
      underlying.encodeTo(value.values, b)
      elementSize * value.values.length
    }

    override def computeSize(value: Arr): Int = {
      value.values.length * elementSize
    }

    override def decode(it: DataInput): Arr = {
      Arr(Vector.from(underlying.decode(it)))
    }
  }

  // tables of primitive types

  @inline implicit def FixedSizeTableCodec[T: ClassTag: EBACodecFixed: DebugTableName]: EBADecoderTable[T] & EBAEncoderTable[T] = new FixedSizeTableCodec[T]

  final class FixedSizeTableCodec[T: ClassTag](implicit elemCodec: EBACodecFixed[T], debugTableName: DebugTableName[T])
    extends EBADecoderTable[T]
    with EBAEncoderTable[T] {

    override def writeTable(stream: OutputStream, table: EBATable[T]): Long = {
      val elemCount = table.size
      val b = ByteString.newBuilder
      IntCodec.encodeTo(elemCount, b)
      var totalTableSize: Long = IntCodec.blobSize.toLong
      table.forEach {
        (elem, _) =>
          elemCodec.encodeTo(elem, b)
          totalTableSize += elemCodec.blobSize
      }
      stream.write(b.result().toArrayUnsafe())
      totalTableSize
    }

    override def readTable(it: DataInput): EBATable[T] = {
      val count = IntCodec.decode(it)
      var i = 0
      val b = new Array[T](count)
      while (i < count) {
        val elem = elemCodec.decode(it)
        b(i) = elem
        i += 1
      }
      new EBATable(debugTableName.tableName, ArraySeq.unsafeWrapArray(b))
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

  object ObjConstants {
    final val noIndexMarker = 65535
    assert(Char.MaxValue == noIndexMarker)

    final val maxIndex = 65534
    assert(maxIndex == noIndexMarker - 1)

    final val range = 0xFFFFFFFFL + 1 // uint max + 1
    assert(range == Math.abs(Integer.MIN_VALUE.toLong) + Integer.MAX_VALUE.toLong + 1)

    @inline final def bucketSize(bucketCount: Short): Long = range / bucketCount
  }

  final class ObjCodec(strings: EBATable[String], packSettings: SICKSettings) extends EBACodecFixedArray[Obj] {
    override def elementSize: Int = ObjectEntryCodec.blobSize

    private val ObjectEntrySeqCodec: EBACodecFixedArray[Seq[(RefVal, Ref)]] = FixedSizeSeqCodec[(RefVal, Ref)](using classTag, ObjectEntryCodec)

    override def encodeTo(value: Obj, bldr: ByteStringBuilder): Int = {
      val bucketCount: Short = packSettings.objectIndexBucketCount
      val indexingThreshold: Short = packSettings.minObjectKeysBeforeIndexing

      val valuesSz = value.values.size

      val objEntriesSortedByCBFHash: Array[((RefVal, Ref), (Long, Int))] = {
        val bucketSize = ObjConstants.bucketSize(bucketCount)
        val arr = new Array[(?, ?)](valuesSz).asInstanceOf[Array[((RefVal, Ref), (Long, Int))]]

        var i = 0
        value.values.foreachEntry {
          (k, v) =>
            val str = strings(k) // fixme string table elements are encoded to utf8 twice, here and in table codec
            val hash = CBFHash.compute(str)
            val bucket = (hash / bucketSize).toInt
            arr(i) = ((k, v), (hash, bucket))
            i += 1
        }
        java.util.Arrays.sort(arr, Ordering.by[((RefVal, Ref), (Long, Int)), Long](_._2._1))
        arr
      }

      if (objEntriesSortedByCBFHash.length >= ObjConstants.maxIndex) {
        throw new RuntimeException(s"Too many keys in object, object can't contain more than ${ObjConstants.noIndexMarker}")
      }

      val index: Array[Int] = {
        if (objEntriesSortedByCBFHash.length <= indexingThreshold) {
          val res = new Array[Int](1); res(0) = ObjConstants.noIndexMarker; res // if the object is small, we put an array with one marker element
        } else {
          val index = Array.fill(bucketCount.toInt)(ObjConstants.maxIndex)
          val sz = objEntriesSortedByCBFHash.length
          var idx = 0
          while (idx < sz) {
            val bucket = objEntriesSortedByCBFHash(idx)._2._2
            val currentVal = index(bucket)
            if (currentVal == ObjConstants.maxIndex) {
              index(bucket) = idx
            }
            idx += 1
          }
          index
        }
      }
      assert(index.length == 1 || index.length == bucketCount)

      if (index.length == bucketCount) {
        var last = objEntriesSortedByCBFHash.length
        var i = bucketCount - 1
        while (i >= 0) {
          if (index(i) == ObjConstants.maxIndex) {
            index(i) = last
          } else {
            last = index(i)
          }
          i = i - 1
        }
      } else if (index.length > 1) {
        throw new IllegalStateException(s"Wrong index size: ${index.length}, expected $bucketCount or 1")
      }

      val oldLength = bldr.length
      index.foreach {
        i =>
          assert(i >= 0 && i <= ObjConstants.noIndexMarker)
          CharCodec.encodeTo(i.toChar, bldr)
      }
      assert(bldr.length == oldLength + Character.BYTES * bucketCount || bldr.length == oldLength + Character.BYTES * 1)

      val objectEntries = {
        val objsCasted = objEntriesSortedByCBFHash.asInstanceOf[Array[(RefVal, Ref)]]
        val sz = objsCasted.length
        var i = 0
        while (i < sz) {
          objsCasted(i) = objEntriesSortedByCBFHash(i)._1
          i += 1
        }
        objsCasted
      }
      ObjectEntrySeqCodec.encodeTo(ArraySeq.unsafeWrapArray(objectEntries), bldr)
      elementSize * valuesSz
    }

    override def computeSize(value: Obj): Int = {
      value.values.size * elementSize
    }

    override def decode(it: DataInput): Obj = {
      val indexMarker = CharCodec.decode(it)

      val entries = if (indexMarker == ObjConstants.noIndexMarker) {
        ObjectEntrySeqCodec.decode(it)
      } else {
        val bucketCount: Short = packSettings.objectIndexBucketCount

        // we don't need the indexHeader when reading the Object whole, so just skip it
        it.skipBytes(Character.BYTES * (bucketCount - 1))

        ObjectEntrySeqCodec.decode(it)
      }

      Obj(mutable.HashMap.from(entries))
    }
  }
  object ObjCodec {
    def apply(strings: EBATable[String], packSettings: SICKSettings): ObjCodec = new ObjCodec(strings, packSettings)
  }

  // tables of variable sized elements

  @inline implicit def VarSizeTableDecoder[T: ClassTag: EBACodecVar: DebugTableName]: EBADecoderTable[T] = new VarSizeTableDecoder[T]

  final class VarSizeTableDecoder[T: ClassTag](implicit varSizeCodec: EBACodecVar[T], debugTableName: DebugTableName[T]) extends EBADecoderTable[T] {
    override def readTable(it: DataInput): EBATable[T] = {
      val elemCount = IntCodec.decode(it)

      val offsets = Array.fill(elemCount + 1)(IntCodec.decode(it))
      val sizes = computeSizesFromOffsets(new mutable.ArraySeq.ofInt(offsets))
      val sizesSz = sizes.length

      val b = new Array[T](sizesSz)
      var i = 0
      while (i < sizesSz) {
        val sz = sizes(i)
        b(i) = varSizeCodec.decode(it, sz)
        i += 1
      }
      new EBATable[T](debugTableName.tableName, ArraySeq.unsafeWrapArray(b))
    }
  }

  // tables of variable sized arrays of primitives

  @inline implicit def FixedSizeArrayTableDecoder[T: ClassTag: EBACodecFixedArray: DebugTableName]: EBADecoderTable[T] = new FixedSizeArrayTableDecoder[T]

  final class FixedSizeArrayTableDecoder[T: ClassTag](implicit fxArrCodec: EBACodecFixedArray[T], debugTableName: DebugTableName[T]) extends EBADecoderTable[T] {
    override def readTable(it: DataInput): EBATable[T] = {
      val elemCount = IntCodec.decode(it)

      // ignore offsets when reading table fully in-memory
      it.skipBytes(IntCodec.blobSize * (elemCount + 1))

      val b = new Array[T](elemCount)
      var i = 0
      while (i < elemCount) {
        b(i) = fxArrCodec.decode(it)
        i += 1
      }
      new EBATable[T](debugTableName.tableName, ArraySeq.unsafeWrapArray(b))
    }
  }

}
