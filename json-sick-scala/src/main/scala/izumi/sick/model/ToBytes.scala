package izumi.sick.model

import izumi.sick.indexes.PackSettings
import izumi.sick.model.Ref.RefVal
import izumi.sick.tables.RefTableRO
import izumi.sick.thirdparty.akka.util.ByteString

import java.nio.ByteBuffer
import java.nio.charset.StandardCharsets
import scala.collection.mutable

sealed trait ToBytes[T] {
  def bytes(value: T): ByteString
}

sealed trait ToBytesFixed[T] extends ToBytes[T] {
  def blobSize: Int
}
sealed trait ToBytesFixedArray[T] extends ToBytes[T] {
  def elementSize: Int
}

sealed trait ToBytesVar[T] extends ToBytes[T]
sealed trait ToBytesVarArray[T] extends ToBytes[T]

@SuppressWarnings(Array("UnsafeTraversableMethods"))
object ToBytes {
  def computeOffsets(collections: Seq[ByteString], initial: Int): Seq[Int] = {
    val out = collections
      .map(_.length)
      .foldLeft(Vector(initial)) {
        case (offsets, currentSize) =>
          offsets :+ (offsets.last + currentSize)
      }
      .init
    assert(out.size == collections.size)
    out
  }

  implicit class AsBytes[T: ToBytes](value: T) {
    def bytes: ByteString = implicitly[ToBytes[T]].bytes(value)
  }

  implicit object LongToBytes extends ToBytesFixed[Long] {
    override def blobSize: Int = java.lang.Long.BYTES

    override def bytes(value: Long): ByteString = {
      val bb = ByteBuffer.allocate(blobSize)
      bb.putLong(value)
      ByteString(bb.array())
    }
  }

  implicit object IntToBytes extends ToBytesFixed[Int] {
    override def blobSize: Int = java.lang.Integer.BYTES

    override def bytes(value: Int): ByteString = {
      val bb = ByteBuffer.allocate(blobSize)
      bb.putInt(value)
      ByteString(bb.array())
    }
  }

  implicit object ShortToBytes extends ToBytesFixed[Short] {
    override def blobSize: Int = java.lang.Short.BYTES

    override def bytes(value: Short): ByteString = {
      val bb = ByteBuffer.allocate(blobSize)
      bb.putShort(value)
      ByteString(bb.array())
    }
  }

  implicit object CharToBytes extends ToBytesFixed[Char] {
    override def blobSize: Int = java.lang.Character.BYTES

    override def bytes(value: Char): ByteString = {
      val bb = ByteBuffer.allocate(blobSize)
      bb.putChar(value)
      ByteString(bb.array())
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
      val bb = ByteBuffer.allocate(blobSize)
      bb.putFloat(value)
      ByteString(bb.array())
    }
  }

  implicit object DoubleToBytes extends ToBytesFixed[Double] {
    override def blobSize: Int = java.lang.Double.BYTES

    override def bytes(value: Double): ByteString = {
      val bb = ByteBuffer.allocate(blobSize)
      bb.putDouble(value)
      ByteString(bb.array())
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

  implicit def toBytesFixedSize[T: ToBytesFixed]: ToBytesFixedArray[Seq[T]] = new ToBytesFixedArray[Seq[T]] {
    override def elementSize: Int = implicitly[ToBytesFixed[T]].blobSize

    override def bytes(value: Seq[T]): ByteString = {
      value.map(_.bytes).foldLeft(value.length.bytes)(_ ++ _)
    }
  }

  implicit def toBytesVarSize[T: ToBytesVar]: ToBytesVarArray[Seq[T]] = new ToBytesVarArray[Seq[T]] {
    override def bytes(value: Seq[T]): ByteString = {
      val arrays = value.map(_.bytes)
      val offsets = computeOffsets(arrays, 0)
      val header = offsets.map(_.bytes).foldLeft(value.length.bytes)(_ ++ _)
      val data = arrays.foldLeft(offsets.lastOption.map(lastOffset => lastOffset + arrays.last.length).getOrElse(0).bytes)(_ ++ _)
      header ++ data
    }
  }

  implicit def toBytesFixedSizeArray[T: ToBytesFixedArray]: ToBytesVarArray[Seq[T]] = new ToBytesVarArray[Seq[T]] {
    override def bytes(value: Seq[T]): ByteString = {
      val arrays = value.map(_.bytes)
      val offsets = computeOffsets(arrays, 0)
      val header = offsets.map(_.bytes).foldLeft(value.length.bytes)(_ ++ _)
      val data = arrays.foldLeft(offsets.lastOption.map(lastOffset => lastOffset + arrays.last.length).getOrElse(0).bytes)(_ ++ _)
      header ++ data
    }
  }

  implicit object StringToBytes extends ToBytesVar[String] {
    override def bytes(value: String): ByteString = {
      ByteString(value.getBytes(StandardCharsets.UTF_8))
    }
  }

  implicit object BigIntToBytes extends ToBytesVar[BigInt] {
    override def bytes(value: BigInt): ByteString = {
      ByteString(value.toByteArray)
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

  class ObjToBytes(strings: RefTableRO[String], packSettings: PackSettings) extends ToBytesFixedArray[Obj] {
    override def elementSize: RefVal = implicitly[ToBytesFixed[(RefVal, Ref)]].blobSize

    @SuppressWarnings(Array("UnnecessaryConversion"))
    override def bytes(value: Obj): ByteString = {
      val bucketCount: Short = packSettings.bucketCount
      val limit: Short = packSettings.limit
      val range = Math.abs(Integer.MIN_VALUE.toLong) + Integer.MAX_VALUE.toLong + 1
      val bucketSize = range / bucketCount

      val hashed = value.values.map {
        case (k, v) =>
          val kval = strings(k)
          val hash = KHash.compute(kval)
          val bucket = (hash / bucketSize).toInt
          ((k, v), (hash, bucket))
      }

      val sorted = hashed.sortBy(_._2._1)
      val data = sorted.map(_._1)
      val buckets = sorted.map(_._2._2).zipWithIndex
      val noIndex = 65535
      assert(noIndex <= Char.MaxValue)
      val maxIndex = noIndex - 1

      if (sorted.size >= maxIndex) {
        throw new RuntimeException(s"Too many keys in object, object can't contain more than $noIndex")
      }

      val index: Seq[Int] = if (sorted.size <= limit) {
        Seq(noIndex)
      } else {
        val startIndexes = mutable.ArrayBuffer.fill(bucketCount)(maxIndex)
        buckets.foreach {
          case (bucket, index) =>
            val currentVal = startIndexes(bucket)
            if (currentVal == maxIndex) {
              startIndexes(bucket) = index
            }
        }

        startIndexes.toSeq
      }
      assert(index.length == 1 || index.length == bucketCount)

      val shortIndex = index.map {
        i =>
          assert(i >= 0 && i <= noIndex)
          i.toChar
      }
      val bytesWithHeader = shortIndex.bytes
      assert(bytesWithHeader.size == Integer.BYTES + Character.BYTES * bucketCount || bytesWithHeader.size == Integer.BYTES + Character.BYTES)

      val indexHeader = bytesWithHeader.drop(java.lang.Integer.BYTES)

      assert(
        indexHeader.size == Character.BYTES * bucketCount || indexHeader.size == Character.BYTES
      )
      indexHeader ++ (data: Seq[(RefVal, Ref)]).bytes
    }
  }

  implicit object RootToBytes extends ToBytesFixed[Root] {
    override def blobSize: RefVal = implicitly[ToBytesFixed[(RefVal, Ref)]].blobSize

    override def bytes(value: Root): ByteString = {
      (value.id, value.ref).bytes
    }
  }
}

object KHash {
  def compute(s: String): Long = {
    var a: Int = 0x6BADBEEF
    s.getBytes(StandardCharsets.UTF_8).foreach {
      b =>
        a ^= a << 13
        a += (a ^ b) << 8
    }
    Integer.toUnsignedLong(a)
  }
}
