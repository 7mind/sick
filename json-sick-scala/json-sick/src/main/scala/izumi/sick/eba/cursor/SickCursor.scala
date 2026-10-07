package izumi.sick.eba.cursor

import izumi.sick.eba.reader.IncrementalEBAReader
import izumi.sick.model.RefKind.*
import izumi.sick.model.{Ref, RefKind}

abstract class SickCursor {
  def ref: Ref
  def ebaReader: IncrementalEBAReader

  def downField(field: String): ObjectCursor = {
    if (ref.kind != TArr) {
      val newRef = ebaReader.readObjectFieldRef(ref, field)
      new ObjectCursor(newRef, ebaReader)
    } else throw new IllegalArgumentException("Ref has an array kind")
  }

  def downArray: ArrayCursor = {
    if (ref.kind == TArr) {
      new ArrayCursor(ref, ebaReader)
    } else throw new IllegalArgumentException("Ref is not an array kind")
  }

  def asObjectCursor: ObjectCursor = {
    this.asInstanceOf[ObjectCursor]
  }

  def asNul: Option[Null] = {
    if (ref.kind == TNul) Some(null)
    else None
  }

  def asBool: Option[Boolean] = {
    if (ref.kind == TBit) Some(ref.ref == 1)
    else None
  }

  def asByte: Option[Byte] = {
    if (ref.kind == TByte) Some(ref.ref.toByte)
    else None
  }

  def asShort: Option[Short] = {
    ref.kind match {
      case RefKind.TByte  => Some(ref.ref.toShort)
      case RefKind.TShort => Some(ref.ref.toShort)
      case _ => None
    }
  }

  def asInt: Option[Int] = {
    ref.kind match {
      case RefKind.TByte  => Some(ref.ref.toInt)
      case RefKind.TShort => Some(ref.ref.toInt)
      case RefKind.TInt   => Some(ebaReader.intTable.readElem(ref.ref))
      case _ => None
    }
  }

  def asLong: Option[Long] = {
    ref.kind match {
      case RefKind.TByte  => Some(ref.ref.toLong)
      case RefKind.TShort => Some(ref.ref.toLong)
      case RefKind.TInt   => Some(ebaReader.intTable.readElem(ref.ref).toLong)
      case RefKind.TLng   => Some(ebaReader.longTable.readElem(ref.ref))
      case _ => None
    }
  }

  def asBigInt: Option[BigInt] = {
    ref.kind match {
      case RefKind.TByte  => Some(BigInt.apply(ref.ref.toLong))
      case RefKind.TShort => Some(BigInt.apply(ref.ref.toLong))
      case RefKind.TInt =>
        Some(BigInt.apply(ebaReader.intTable.readElem(ref.ref).toLong))
      case RefKind.TLng => Some(BigInt.apply(ebaReader.longTable.readElem(ref.ref)))
      case RefKind.TBigInt => Some(ebaReader.bigIntTable.readElem(ref.ref))
      case _ => None
    }
  }

  def asFloat: Option[Float] = {
    ref.kind match {
      case RefKind.TByte  => Some(ref.ref.toFloat)
      case RefKind.TShort => Some(ref.ref.toFloat)
      case RefKind.TInt   => Some(ebaReader.intTable.readElem(ref.ref).toFloat)
      case RefKind.TLng   => Some(ebaReader.longTable.readElem(ref.ref).toFloat)
      case RefKind.TFlt   => Some(ebaReader.floatTable.readElem(ref.ref))
      case _ => None
    }
  }

  def asDouble: Option[Double] = {
    ref.kind match {
      case RefKind.TByte  => Some(ref.ref.toDouble)
      case RefKind.TShort => Some(ref.ref.toDouble)
      case RefKind.TInt   => Some(ebaReader.intTable.readElem(ref.ref).toDouble)
      case RefKind.TLng   => Some(ebaReader.longTable.readElem(ref.ref).toDouble)
      case RefKind.TFlt   => Some(ebaReader.floatTable.readElem(ref.ref).toDouble)
      case RefKind.TDbl   => Some(ebaReader.doubleTable.readElem(ref.ref))
      case _ => None
    }
  }

  def asBigDec: Option[BigDecimal] = {
    ref.kind match {
      case RefKind.TByte  => Some(BigDecimal.apply(ref.ref.toLong))
      case RefKind.TShort => Some(BigDecimal.apply(ref.ref.toLong))
      case RefKind.TInt =>
        Some(BigDecimal.apply(ebaReader.intTable.readElem(ref.ref).toLong))
      case RefKind.TLng =>
        Some(BigDecimal.apply(ebaReader.longTable.readElem(ref.ref)))
      case RefKind.TFlt =>
        Some(BigDecimal.apply(ebaReader.floatTable.readElem(ref.ref).toDouble))
      case RefKind.TDbl =>
        Some(BigDecimal.apply(ebaReader.doubleTable.readElem(ref.ref)))
      case RefKind.TBigDec => Some(ebaReader.bigDecTable.readElem(ref.ref))
      case _ => None
    }
  }

  def asString: Option[String] = {
    if (ref.kind == TStr) Some(ebaReader.strTable.readElem(ref.ref))
    else None
  }
}