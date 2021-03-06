package izumi.sick.model

import akka.util.ByteString
import izumi.sick.model.Ref.RefVal

import java.nio.ByteBuffer
import java.nio.charset.StandardCharsets

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

    override def bytes(value: Arr): ByteString = {
      (value.values.toSeq: Seq[Ref]).bytes
    }
  }

  implicit object ObjToBytes extends ToBytesFixedArray[Obj] {
    override def elementSize: RefVal = implicitly[ToBytesFixed[(RefVal, Ref)]].blobSize

    override def bytes(value: Obj): ByteString = {
      (value.values.toSeq: Seq[(RefVal, Ref)]).bytes
    }
  }

  implicit object RootToBytes extends ToBytesFixed[Root] {
    override def blobSize: RefVal = implicitly[ToBytesFixed[(RefVal, Ref)]].blobSize

    override def bytes(value: Root): ByteString = {
      (value.id, value.ref).bytes
    }
  }
}
