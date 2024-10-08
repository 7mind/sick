package izumi.sick.eba.writer

import izumi.sick.eba.{EBATable, SICKSettings}
import izumi.sick.model.Ref.RefVal
import izumi.sick.model.*
import izumi.sick.thirdparty.akka.util.ByteString
import izumi.sick.tools.CBFHash

import java.io.{FileOutputStream, OutputStream}
import java.nio.ByteBuffer
import java.nio.charset.StandardCharsets
import scala.collection.mutable

sealed trait ToBytes[T] {
  def bytes(value: T): ByteString
}

sealed trait ToBytesFixed[T] extends ToBytes[T] {
  def blobSize: Int
}

sealed trait ToBytesTable[T] {
  def write(stream: OutputStream, table: EBATable[T]): Long
}

sealed trait ToBytesFixedArray[T] extends ToBytes[T] {
  def elementSize: Int
}

sealed trait ToBytesVar[T] extends ToBytes[T]

@SuppressWarnings(Array("UnsafeTraversableMethods"))
class EBAWriter(params: SICKWriterParameters) {
//  def computeOffsets(collections: Seq[ByteString], initial: Int): Seq[Int] = {
//    computeOffsetsFromSizes(collections.map(_.length), initial)
//  }

  private val arrayWriter = params.arrayWriteStrategy match {
    case ArrayWriteStrategy.StreamRepositioning =>
      ArrayWriter.StreamPositioning
    case ArrayWriteStrategy.SinglePassInMemory =>
      ArrayWriter.SinglePassInMemory
    case ArrayWriteStrategy.DoublePass =>
      ArrayWriter.DoublePass

  }
  def computeOffsetsFromSizes(lengths: Seq[Int], initial: Int): Seq[Int] = {
    val out = lengths
      .foldLeft(Vector(initial)) {
        case (offsets, currentSize) =>
          offsets :+ (offsets.last + currentSize)
      }
      .init
    assert(out.size == lengths.size)
    out
  }

  private def fromByteBuffer(blobSize: Int)(b: ByteBuffer => Any): ByteString = {
    val bb = ByteBuffer.allocate(blobSize)
    b(bb)
    ByteString.fromArrayUnsafe(bb.array())
  }

  implicit class AsBytes[T: ToBytes](value: T) {
    def bytes: ByteString = implicitly[ToBytes[T]].bytes(value)
  }

  implicit object LongToBytes extends ToBytesFixed[Long] {
    override def blobSize: Int = java.lang.Long.BYTES

    override def bytes(value: Long): ByteString = {
      fromByteBuffer(blobSize)(_.putLong(value))
    }
  }

  implicit object IntToBytes extends ToBytesFixed[Int] {
    override def blobSize: Int = java.lang.Integer.BYTES

    override def bytes(value: Int): ByteString = {
      fromByteBuffer(blobSize)(_.putInt(value))
    }
  }

  implicit object ShortToBytes extends ToBytesFixed[Short] {
    override def blobSize: Int = java.lang.Short.BYTES

    override def bytes(value: Short): ByteString = {
      fromByteBuffer(blobSize)(_.putShort(value))
    }
  }

  implicit object CharToBytes extends ToBytesFixed[Char] {
    override def blobSize: Int = java.lang.Character.BYTES

    override def bytes(value: Char): ByteString = {
      fromByteBuffer(blobSize)(_.putChar(value))
    }
  }

  implicit object ByteToBytes extends ToBytesFixed[Byte] {
    override def blobSize: Int = 1

    override def bytes(value: Byte): ByteString = {
      ByteString(value)
    }
  }

  implicit object FloatToBytes extends ToBytesFixed[Float] {
    override def blobSize: Int = java.lang.Float.BYTES

    override def bytes(value: Float): ByteString = {
      fromByteBuffer(blobSize)(_.putFloat(value))
    }
  }

  implicit object DoubleToBytes extends ToBytesFixed[Double] {
    override def blobSize: Int = java.lang.Double.BYTES

    override def bytes(value: Double): ByteString = {
      fromByteBuffer(blobSize)(_.putDouble(value))
    }
  }

  implicit object RefKindBytes extends ToBytesFixed[RefKind] {
    override def blobSize: Int = 1

    override def bytes(value: RefKind): ByteString = {
      value.index.bytes
    }
  }

  implicit object ArrayEntryBytes extends ToBytesFixed[Ref] {
    override def blobSize: Int = 1 + Integer.BYTES

    override def bytes(value: Ref): ByteString = {
      val out = value.kind.bytes ++ value.ref.bytes
      assert(out.size == blobSize)
      out
    }
  }

  implicit object ObjectEntryBytes extends ToBytesFixed[(RefVal, Ref)] {
    override def blobSize: Int = Integer.BYTES * 2 + 1

    override def bytes(value: (RefVal, Ref)): ByteString = {
      val out = value._1.bytes ++ value._2.bytes
      assert(out.size == blobSize)
      out
    }
  }

  implicit def toBytesFixedSize[T: ToBytesFixed]: ToBytesFixedArray[Seq[T]] with ToBytesTable[T] = new ToBytesFixedArray[Seq[T]] with ToBytesTable[T] {
    override def elementSize: Int = implicitly[ToBytesFixed[T]].blobSize

    override def bytes(value: Seq[T]): ByteString = {
      value.foldLeft(value.length.bytes) { case (acc, v) => acc ++ v.bytes }
    }

    override def write(stream: OutputStream, table: EBATable[T]): Long = {
      val sz = table.size.bytes
      var added: Long = sz.length
      stream.write(sz.toArray)
      table.forEach {
        s =>
          val el = s.bytes
          stream.write(el.toArray)
          added += el.size
      }
      added
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
//
//    }
  }

  implicit def toBytesVarSize[T: ToBytesVar]: ToBytesTable[T] = new ToBytesTable[T] {
    override def write(stream: OutputStream, table: EBATable[T]): Long = {
      doWriteArray(stream, table, implicitly[ToBytesVar[T]])
    }
  }

  implicit def toBytesFixedSizeArray[T: ToBytesFixedArray]: ToBytesTable[T] = new ToBytesTable[T] {
    override def write(stream: OutputStream, table: EBATable[T]): Long = {
      doWriteArray(stream, table, implicitly[ToBytesFixedArray[T]])
    }
  }

  implicit object StringToBytes extends ToBytesVar[String] {
    override def bytes(value: String): ByteString = {
      ByteString.fromArrayUnsafe(value.getBytes(StandardCharsets.UTF_8))
    }
  }

  implicit object BigIntToBytes extends ToBytesVar[BigInt] {
    override def bytes(value: BigInt): ByteString = {
      ByteString.fromArrayUnsafe(value.toByteArray)
    }
  }

  implicit object BigDecimalToBytes extends ToBytesVar[BigDecimal] {
    override def bytes(value: BigDecimal): ByteString = {

      value.underlying().signum().bytes ++ value.underlying().precision().bytes ++ value.underlying().scale().bytes ++ ByteString(
        value.underlying().unscaledValue().toByteArray
      )
    }
  }

  implicit object ArrToBytes extends ToBytesFixedArray[Arr] {
    override def elementSize: RefVal = implicitly[ToBytesFixed[Ref]].blobSize

    @SuppressWarnings(Array("UnnecessaryConversion"))
    override def bytes(value: Arr): ByteString = {
      (value.values.toSeq: Seq[Ref]).bytes
    }
  }

  class ObjToBytes(strings: EBATable[String], packSettings: SICKSettings) extends ToBytesFixedArray[Obj] {
    override def elementSize: RefVal = implicitly[ToBytesFixed[(RefVal, Ref)]].blobSize

    @SuppressWarnings(Array("UnnecessaryConversion"))
    override def bytes(value: Obj): ByteString = {
      val bucketCount: Short = packSettings.objectIndexBucketCount
      val indexingThreshold: Short = packSettings.minObjectKeysBeforeIndexing
      val range = Math.abs(Integer.MIN_VALUE.toLong) + Integer.MAX_VALUE.toLong + 1
      val bucketSize = range / bucketCount

      val sortedByHash = value.values
        .map {
          case (k, v) =>
            val kval = strings(k)
            val hash = CBFHash.compute(kval)
            val bucket = (hash / bucketSize).toInt
            ((k, v), (hash, bucket))
        }.sortBy(_._2._1)

      val noIndexMarker = 65535
      assert(noIndexMarker <= Char.MaxValue)
      val maxIndex = noIndexMarker - 1

      if (sortedByHash.size >= maxIndex) {
        throw new RuntimeException(s"Too many keys in object, object can't contain more than $noIndexMarker")
      }

      val index: mutable.ArrayBuffer[Int] = if (sortedByHash.size <= indexingThreshold) {
        mutable.ArrayBuffer(noIndexMarker) // if the object is small, we put an array with one marker element
      } else {
        val startIndexes = mutable.ArrayBuffer.fill(bucketCount.toInt)(maxIndex)
        var idx = 0
        sortedByHash.foreach {
          case ((_, _), (_, bucket)) =>
            val currentVal = startIndexes(bucket)
            if (currentVal == maxIndex) {
              startIndexes(bucket) = idx
            }
            idx += 1
        }

        startIndexes
      }
      assert(index.length == 1 || index.length == bucketCount)

      if (index.length == bucketCount) {
        var last = sortedByHash.size
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

      val shortIndex = index.map {
        i =>
          assert(i >= 0 && i <= noIndexMarker)
          i.toChar
      }.toSeq
      val bytesWithHeader = shortIndex.bytes
      assert(bytesWithHeader.size == Integer.BYTES + Character.BYTES * bucketCount || bytesWithHeader.size == Integer.BYTES + Character.BYTES)

      val indexHeader = bytesWithHeader.drop(java.lang.Integer.BYTES)

      assert(
        indexHeader.size == Character.BYTES * bucketCount || indexHeader.size == Character.BYTES
      )
      indexHeader ++ (sortedByHash.map(_._1): Seq[(RefVal, Ref)]).bytes
    }
  }

  implicit object RootToBytes extends ToBytesFixed[Root] {
    override def blobSize: RefVal = implicitly[ToBytesFixed[(RefVal, Ref)]].blobSize

    override def bytes(value: Root): ByteString = {
      (value.id, value.ref).bytes
    }
  }

  private def doWriteArray[T](stream: OutputStream, table: EBATable[T], codec: ToBytes[T]): Long = {
    arrayWriter.writeArray(stream, table, codec, this)
  }

}
