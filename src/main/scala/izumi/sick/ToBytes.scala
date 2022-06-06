package izumi.sick

import akka.util.ByteString
import izumi.sick.Ref.RefVal

import java.nio.ByteBuffer
import java.nio.charset.StandardCharsets

trait ToBytes[T] {
  def bytes(value: T): ByteString
}

object ToBytes {
  implicit class AsBytes[T : ToBytes](value: T) {
    def bytes: ByteString = implicitly[ToBytes[T]].bytes(value)
  }

  implicit object RefKindBytes extends ToBytes[RefKind] {
    override def bytes(value: RefKind): ByteString = {
      value.index.bytes
    }
  }

  implicit object ArrayEntryBytes extends ToBytes[Ref] {
    override def bytes(value: Ref): ByteString = {
      val out = value.kind.bytes ++ value.ref.bytes
      assert(out.size == Integer.BYTES * 2)
      out
    }
  }

  implicit object ObjectEntryBytes extends ToBytes[(RefVal, Ref)] {
    override def bytes(value: (RefVal, Ref)): ByteString = {
      val out = value._1.bytes ++ value._2.bytes
      assert(out.size == Integer.BYTES*3)
      out
    }
  }

  implicit object LongToBytes extends ToBytes[Long] {
    override def bytes(value: Long): ByteString = {
      val bb = ByteBuffer.allocate(java.lang.Long.BYTES)
      bb.putLong(value)
      ByteString(bb.array())
    }
  }

  implicit object IntToBytes extends ToBytes[Int] {
    override def bytes(value: Int): ByteString = {
      val bb = ByteBuffer.allocate(java.lang.Integer.BYTES)
      bb.putInt(value)
      ByteString(bb.array())
    }
  }

  implicit object ShortToBytes extends ToBytes[Short] {
    override def bytes(value: Short): ByteString = {
      val bb = ByteBuffer.allocate(java.lang.Short.BYTES)
      bb.putShort(value)
      ByteString(bb.array())
    }
  }

  implicit object FloatToBytes extends ToBytes[Float] {
    override def bytes(value: Float): ByteString = {
      val bb = ByteBuffer.allocate(java.lang.Float.BYTES)
      bb.putFloat(value)
      ByteString(bb.array())
    }
  }

  implicit object DoubleToBytes extends ToBytes[Double] {
    override def bytes(value: Double): ByteString = {
      val bb = ByteBuffer.allocate(java.lang.Double.BYTES)
      bb.putDouble(value)
      ByteString(bb.array())
    }
  }

  implicit def toBytesFixedSize[T: ToBytes]:  ToBytes[Seq[T]]  = new ToBytes[Seq[T]] {
    override def bytes(value: Seq[T]): ByteString = {
      value.map(_.bytes).foldLeft(value.length.bytes)(_ ++ _)
    }
  }
}

trait ToBytesVar[T] extends ToBytes[T] {
  //  def byteLength(value: T): Int = bytes(value).length
}

object ToBytesVar {
  import ToBytes.{AsBytes, ArrayEntryBytes, toBytesFixedSize}


  implicit def toBytesVarSize[T: ToBytesVar]:  ToBytes[Seq[T]]  = new ToBytes[Seq[T]] {
    override def bytes(value: Seq[T]): ByteString = {
      val arrays = value.map(_.bytes)
      val header = arrays.map(_.length.bytes).foldLeft(value.length.bytes)(_ ++ _)
      val data = arrays.foldLeft(value.length.bytes)(_ ++ _)
      header ++ data
    }
  }

  implicit object StringToBytes extends ToBytesVar[String] {
    override def bytes(value: String): ByteString = {
      ByteString(value.getBytes(StandardCharsets.UTF_8))
    }
  }

  implicit object ArrToBytes extends ToBytesVar[Arr] {
    override def bytes(value: Arr): ByteString = {
      (value.values.toSeq:Seq[Ref]).bytes
    }
  }

  implicit object ObjToBytes extends ToBytesVar[Obj] {
    override def bytes(value: Obj): ByteString = {
      (value.values.toSeq:Seq[(RefVal, Ref)]).bytes
    }
  }

}