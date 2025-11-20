package izumi.sick.eba.cursor

import izumi.sick.eba.reader.IncrementalEBAReader
import izumi.sick.model.{Arr, Obj, Ref, RefKind, Root}
import izumi.sick.model.RefKind.*

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

  def asNul: Null = {
    if (ref.kind == TNul) null
    else throw new RuntimeException("Ref kind was not TNul")
  }

  def asBool: Boolean = {
    if (ref.kind == TBit) ref.ref == 1
    else throw new RuntimeException("Ref kind was not TBit")
  }

  def asByte: Byte = {
    if (ref.kind == TByte) ref.ref.toByte
    else throw new RuntimeException("Ref kind was not TByte")
  }

  def asShort: Short = {
    ref.kind match {
      case RefKind.TByte  => ref.ref.toShort
      case RefKind.TShort => ref.ref.toShort
      case _ => throw new RuntimeException("Can not cast ref value to Short")
    }
  }

  def asInt: Int = {
    ref.kind match {
      case RefKind.TByte  => ref.ref.toInt
      case RefKind.TShort => ref.ref.toInt
      case RefKind.TInt   => ebaReader.intTable.readElem(ref.ref)
      case _ => throw new RuntimeException("Can not cast ref value to Int")
    }
  }

  def asLong: Long = {
    ref.kind match {
      case RefKind.TByte  => ref.ref.toLong
      case RefKind.TShort => ref.ref.toLong
      case RefKind.TInt   => ebaReader.intTable.readElem(ref.ref).toLong
      case RefKind.TLng   => ebaReader.longTable.readElem(ref.ref)
      case _ => throw new RuntimeException("Can not cast ref value to Long")
    }
  }

  def asBigInt: BigInt = {
    ref.kind match {
      case RefKind.TByte  => BigInt.apply(ref.ref.toLong)
      case RefKind.TShort => BigInt.apply(ref.ref.toLong)
      case RefKind.TInt =>
        BigInt.apply(ebaReader.intTable.readElem(ref.ref).toLong)
      case RefKind.TLng => BigInt.apply(ebaReader.longTable.readElem(ref.ref))
      case RefKind.TBigInt => ebaReader.bigIntTable.readElem(ref.ref)
      case kind => throw new RuntimeException(s"Can not cast ref value of kind $kind to BigInt")
    }
  }

  def asFloat: Float = {
    ref.kind match {
      case RefKind.TByte  => ref.ref.toFloat
      case RefKind.TShort => ref.ref.toFloat
      case RefKind.TInt   => ebaReader.intTable.readElem(ref.ref).toFloat
      case RefKind.TLng   => ebaReader.longTable.readElem(ref.ref).toFloat
      case RefKind.TFlt   => ebaReader.floatTable.readElem(ref.ref)
      case kind => throw new RuntimeException(s"Can not cast ref value of kind $kind to Float")
    }
  }

  def asDouble: Double = {
    ref.kind match {
      case RefKind.TByte  => ref.ref.toDouble
      case RefKind.TShort => ref.ref.toDouble
      case RefKind.TInt   => ebaReader.intTable.readElem(ref.ref).toDouble
      case RefKind.TLng   => ebaReader.longTable.readElem(ref.ref).toDouble
      case RefKind.TFlt   => ebaReader.floatTable.readElem(ref.ref).toDouble
      case RefKind.TDbl   => ebaReader.doubleTable.readElem(ref.ref)
      case kind => throw new RuntimeException(s"Can not cast ref value of kind $kind to Double")
    }
  }

  def asBigDec: BigDecimal = {
    ref.kind match {
      case RefKind.TByte  => BigDecimal.apply(ref.ref.toLong)
      case RefKind.TShort => BigDecimal.apply(ref.ref.toLong)
      case RefKind.TInt =>
        BigDecimal.apply(ebaReader.intTable.readElem(ref.ref).toLong)
      case RefKind.TLng =>
        BigDecimal.apply(ebaReader.longTable.readElem(ref.ref))
      case RefKind.TFlt =>
        BigDecimal.apply(ebaReader.floatTable.readElem(ref.ref).toDouble)
      case RefKind.TDbl =>
        BigDecimal.apply(ebaReader.doubleTable.readElem(ref.ref))
      case RefKind.TBigDec => ebaReader.bigDecTable.readElem(ref.ref)
      case kind => throw new RuntimeException(s"Can not cast ref value of kind $kind to BigDec")
    }
  }

  def asString: String = {
    if (ref.kind == TStr) ebaReader.strTable.readElem(ref.ref)
    else throw new RuntimeException("Ref kind was not TStr")
  }

  def asArray: Arr = {
    if (ref.kind == TArr)
      Arr(ebaReader.arrTable.readElem(ref.ref).readAll().toVector)
    else throw new RuntimeException("Ref kind was not TArr")
  }

  def asObject: Obj = {
    if (ref.kind == TObj) ebaReader.objTable.readElem(ref.ref).readAllObj()
    else throw new RuntimeException("Ref kind was not TObj")
  }

  def asRoot: Root = {
    if (ref.kind == TRoot) ebaReader.rootTable.readElem(ref.ref)
    else throw new RuntimeException("Ref kind was not TRoot")
  }
}