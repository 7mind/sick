package izumi.sick.eba.writer.codecs

import izumi.sick.eba.writer.codecs.EBACodecs.{EBACodecFixedArray, EBACodecVar, EBAEncoderTable}
import izumi.sick.eba.{EBATable, SICKSettings}
import izumi.sick.model.*
import izumi.sick.model.Ref.RefVal
import izumi.sick.thirdparty.akka.util.ByteString
import izumi.sick.tools.CBFHash

import java.io.{DataInput, OutputStream}
import java.math.{BigInteger, MathContext}
import java.nio.charset.StandardCharsets
import java.nio.{ByteBuffer, ByteOrder}
import scala.annotation.unused
import scala.collection.immutable.{ArraySeq, HashMap}
import scala.collection.mutable
import scala.reflect.{ClassTag, classTag}
import izumi.sick.eba.writer.codecs.util.computeSizesFromOffsets

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
    def encode(value: T): ByteString
  }

  abstract class EBACodecFixed[T] extends EBAEncoder[T] {
    def blobSize: Int

    def decode(it: DataInput): T
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

    override def encode(value: Byte): ByteString = {
      ByteString(value)
    }

    override def decode(it: DataInput): Byte = {
      it.readByte()
    }
  }

  implicit object ShortCodec extends EBACodecFixed[Short] {
    override final val blobSize = java.lang.Short.BYTES

    override def encode(value: Short): ByteString = {
      fromByteBuffer(blobSize)(_.putShort(value))
    }

    override def decode(it: DataInput): Short = {
      it.readShort()
    }
  }

  // unsigned Short
  implicit object CharCodec extends EBACodecFixed[Char] {
    override final val blobSize = java.lang.Character.BYTES

    override def encode(value: Char): ByteString = {
      fromByteBuffer(blobSize)(_.putChar(value))
    }

    override def decode(it: DataInput): Char = {
      it.readChar()
    }
  }

  implicit object IntCodec extends EBACodecFixed[Int] {
    override final val blobSize = java.lang.Integer.BYTES

    override def encode(value: Int): ByteString = {
      fromByteBuffer(blobSize)(_.putInt(value))
    }

    override def decode(it: DataInput): Int = {
      it.readInt()
    }
  }

  @inline implicit final def RefValCodec: EBACodecFixed[RefVal] = IntCodec.asInstanceOf[EBACodecFixed[RefVal]]

  implicit object LongCodec extends EBACodecFixed[Long] {
    override final val blobSize = java.lang.Long.BYTES

    override def encode(value: Long): ByteString = {
      fromByteBuffer(blobSize)(_.putLong(value))
    }

    override def decode(it: DataInput): Long = {
      it.readLong()
    }
  }

  implicit object FloatCodec extends EBACodecFixed[Float] {
    override final val blobSize = java.lang.Float.BYTES

    override def encode(value: Float): ByteString = {
      fromByteBuffer(blobSize)(_.putFloat(value))
    }

    override def decode(it: DataInput): Float = {
      it.readFloat()
    }
  }

  implicit object DoubleCodec extends EBACodecFixed[Double] {
    override final val blobSize = java.lang.Double.BYTES

    override def encode(value: Double): ByteString = {
      fromByteBuffer(blobSize)(_.putDouble(value))
    }

    override def decode(it: DataInput): Double = {
      it.readDouble()
    }
  }

  implicit object RefKindCodec extends EBACodecFixed[RefKind] {
    override final val blobSize = 1

    override def encode(value: RefKind): ByteString = {
      ByteCodec.encode(value.index)
    }

    override def decode(it: DataInput): RefKind = {
      RefKind.fromIndex(it.readByte())
    }
  }

  implicit object RefCodec extends EBACodecFixed[Ref] {
    override final val blobSize = 1 + Integer.BYTES

    override def encode(value: Ref): ByteString = {
      val out = RefKindCodec.encode(value.kind) ++ RefValCodec.encode(value.ref)
      assert(out.size == blobSize)
      out
    }

    override def decode(it: DataInput): Ref = {
      val kind = RefKindCodec.decode(it)
      val refVal = RefValCodec.decode(it)
      Ref(kind, Ref.RefVal(refVal))
    }
  }

  implicit object ObjectEntryCodec extends EBACodecFixed[(RefVal, Ref)] {
    override final val blobSize = Integer.BYTES * 2 + 1

    override def encode(value: (RefVal, Ref)): ByteString = {
      val out = RefValCodec.encode(value._1) ++ RefCodec.encode(value._2)
      assert(out.size == blobSize)
      out
    }

    override def decode(it: DataInput): (RefVal, Ref) = {
      val refVal = RefValCodec.decode(it)
      val ref = RefCodec.decode(it)
      (refVal, ref)
    }
  }

  implicit object RootCodec extends EBACodecFixed[Root] {
    override final val blobSize = ObjectEntryCodec.blobSize

    override def encode(value: Root): ByteString = {
      ObjectEntryCodec.encode(value.id, value.ref)
    }

    override def decode(it: DataInput): Root = {
      val tuple = ObjectEntryCodec.decode(it)
      Root(tuple._1, tuple._2)
    }
  }

  // primitive variable-size types

  implicit val StringCodec: EBACodecVar[String] = new EBACodecVar[String] {
    override def encode(value: String): ByteString = {
      ByteString.fromArrayUnsafe(value.getBytes(StandardCharsets.UTF_8))
    }

    override def decode(it: DataInput, length: Int): String = {
      val bytes = new Array[Byte](length)
      it.readFully(bytes, 0, length)
      new String(bytes, 0, length, StandardCharsets.UTF_8)
    }
  }

  implicit val BigIntCodec: EBACodecVar[BigInt] = new EBACodecVar[BigInt] {
    override def encode(value: BigInt): ByteString = {
      ByteString.fromArrayUnsafe(value.toByteArray)
    }

    override def decode(it: DataInput, length: Int): BigInt = {
      val bytes = new Array[Byte](length)
      it.readFully(bytes, 0, length)
      BigInt(bytes)
    }
  }

  implicit val BigDecimalCodec: EBACodecVar[BigDecimal] = new EBACodecVar[BigDecimal] {
    override def encode(value: BigDecimal): ByteString = {
      IntCodec.encode(value.underlying().signum())
        ++ IntCodec.encode(value.underlying().precision())
        ++ IntCodec.encode(value.underlying().scale())
        ++ ByteString(value.underlying().unscaledValue().toByteArray)
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

  implicit def FixedSizeSeqCodec[T: ClassTag](implicit codecFixed: EBACodecFixed[T]): EBACodecFixedArray[Seq[T]] = new EBACodecFixedArray[Seq[T]] {
    override def elementSize: Int = codecFixed.blobSize

    override def encode(value: Seq[T]): ByteString = {
      val b = ByteString.newBuilder
      b.putInt(value.length)(using ByteOrder.BIG_ENDIAN)
      value.foreach(b ++= codecFixed.encode(_))
      b.result()
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

  implicit val ArrCodec: EBACodecFixedArray[Arr] = new EBACodecFixedArray[Arr] {
    private val underlying: EBACodecFixedArray[Seq[Ref]] = FixedSizeSeqCodec(using classTag, RefCodec)

    override def elementSize: Int = RefCodec.blobSize

    override def encode(value: Arr): ByteString = {
      underlying.encode(value.values)
    }

    override def decode(it: DataInput): Arr = {
      Arr(Vector.from(underlying.decode(it)))
    }
  }

  // tables of primitive types

  implicit def FixedSizeTableCodec[T](implicit elemCodec: EBACodecFixed[T], debugTableName: DebugTableName[T]): EBADecoderTable[T] & EBAEncoderTable[T] =
    new EBADecoderTable[T] with EBAEncoderTable[T] {

      override def writeTable(stream: OutputStream, table: EBATable[T]): Long = {
        val elemCount = table.size
        stream.write(IntCodec.encode(elemCount).toArrayUnsafe())
        var totalTableSize: Long = IntCodec.blobSize.toLong
        table.forEach {
          elem =>
            stream.write(elemCodec.encode(elem).toArrayUnsafe())
            totalTableSize += elemCodec.blobSize
        }
        totalTableSize
      }

      override def readTable(it: DataInput): EBATable[T] = {
        val count = IntCodec.decode(it)
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

  object ObjConstants {
    final val noIndexMarker = 65535
    assert(Char.MaxValue == noIndexMarker)

    final val maxIndex = 65534
    assert(maxIndex == noIndexMarker - 1)

    final val range = 0xFFFFFFFFL + 1 // uint max + 1
    assert(range == Math.abs(Integer.MIN_VALUE.toLong) + Integer.MAX_VALUE.toLong + 1)

    @inline final def bucketSize(bucketCount: Short): Long = range / bucketCount
  }

  def ObjCodec(strings: EBATable[String], packSettings: SICKSettings): EBACodecFixedArray[Obj] = new EBACodecFixedArray[Obj] {
    override def elementSize: Int = ObjectEntryCodec.blobSize

    private val ObjectEntrySeqCodec: EBACodecFixedArray[Seq[(RefVal, Ref)]] = FixedSizeSeqCodec[(RefVal, Ref)](using classTag, ObjectEntryCodec)

    @SuppressWarnings(Array("UnnecessaryConversion"))
    override def encode(value: Obj): ByteString = {
      val bucketCount: Short = packSettings.objectIndexBucketCount
      val indexingThreshold: Short = packSettings.minObjectKeysBeforeIndexing

      val objEntriesSortedByCBFHash: ArraySeq[((RefVal, Ref), (Long, Int))] = {
        val bucketSize = ObjConstants.bucketSize(bucketCount)
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

      if (objEntriesSortedByCBFHash.size >= ObjConstants.maxIndex) {
        throw new RuntimeException(s"Too many keys in object, object can't contain more than ${ObjConstants.noIndexMarker}")
      }

      val index: mutable.ArrayBuffer[Int] = {
        if (objEntriesSortedByCBFHash.size <= indexingThreshold) {
          mutable.ArrayBuffer(ObjConstants.noIndexMarker) // if the object is small, we put an array with one marker element
        } else {
          val index = mutable.ArrayBuffer.fill(bucketCount.toInt)(ObjConstants.maxIndex)
          var idx = 0
          objEntriesSortedByCBFHash.foreach {
            case ((_, _), (_, bucket)) =>
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
        var last = objEntriesSortedByCBFHash.size
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

      val indexHeader = index.foldLeft(ByteString.empty) {
        (b, i) =>
          assert(i >= 0 && i <= ObjConstants.noIndexMarker)
          b ++ CharCodec.encode(i.toChar)
      }
      assert(indexHeader.size == Character.BYTES * bucketCount || indexHeader.size == Character.BYTES * 1)

      val objectEntries = objEntriesSortedByCBFHash.map(_._1)
      val objectEntryArray = ObjectEntrySeqCodec.encode(objectEntries)

      indexHeader ++ objectEntryArray
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

      Obj(Map.from(entries))
    }
  }

  // tables of variable sized elements

  implicit def VarSizeTableDecoder[T](implicit varSizeCodec: EBACodecVar[T], debugTableName: DebugTableName[T]): EBADecoderTable[T] = new EBADecoderTable[T] {
    override def readTable(it: DataInput): EBATable[T] = {
      val elemCount = IntCodec.decode(it)

      val offsets = ArraySeq.fill(elemCount + 1) {
        IntCodec.decode(it)
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

  implicit def FixedSizeArrayTableDecoder[T](implicit fxArrCodec: EBACodecFixedArray[T], debugTableName: DebugTableName[T]): EBADecoderTable[T] = new EBADecoderTable[T] {
    override def readTable(it: DataInput): EBATable[T] = {
      val elemCount = IntCodec.decode(it)

      // ignore offsets when reading table fully in-memory
      it.skipBytes(IntCodec.blobSize * (elemCount + 1))

      val b = HashMap.newBuilder[RefVal, T]
      var i = 0
      while (i < elemCount) {
        b += (RefVal(i) -> fxArrCodec.decode(it))
        i += 1
      }
      val elems = b.result()
      new EBATable[T](debugTableName.tableName, elems)
    }
  }

  private def fromByteBuffer(blobSize: Int)(b: ByteBuffer => Any): ByteString = {
    val bb = ByteBuffer.allocate(blobSize)
    b(bb)
    ByteString.fromArrayUnsafe(bb.array())
  }

}
