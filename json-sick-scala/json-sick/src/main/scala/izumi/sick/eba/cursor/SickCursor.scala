package izumi.sick.eba.cursor

import izumi.sick.eba.reader.IncrementalEBAReader
import izumi.sick.eba.reader.incremental.IncrementalJValue.*
import izumi.sick.model.Ref
import izumi.sick.model.RefKind.*

abstract class SickCursor {
  def ref: Ref
  def ebaReader: IncrementalEBAReader

  def downField(field: String): SickCursor = {
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

  def asNul: JNul.type = {
    if (ref.kind == TNul) JNul
    else throw new RuntimeException("Ref kind was not TNul")
  }

  def asBit: JBit = {
    if (ref.kind == TBit) JBit(ref.ref == 1)
    else throw new RuntimeException("Ref kind was not TBit")
  }

  def asByte: JByte = {
    if (ref.kind == TByte) JByte(ref.ref.toByte)
    else throw new RuntimeException("Ref kind was not TByte")
  }

  def asShort: JShort = {
    if (ref.kind == TShort) JShort(ref.ref.toShort)
    else throw new RuntimeException("Ref kind was not TShort")
  }

  def asInt: JInt = {
    if (ref.kind == TInt) JInt(ebaReader.intTable.readElem(ref.ref))
    else throw new RuntimeException("Ref kind was not TInt")
  }

  def asLong: JLong = {
    if (ref.kind == TLng) JLong(ebaReader.longTable.readElem(ref.ref))
    else throw new RuntimeException("Ref kind was not TLng")
  }

  def asBigInt: JBigInt = {
    if (ref.kind == TBigInt) JBigInt(ebaReader.bigIntTable.readElem(ref.ref))
    else throw new RuntimeException("Ref kind was not TBigInt")
  }

  def asFloat: JFloat = {
    if (ref.kind == TFlt) JFloat(ebaReader.floatTable.readElem(ref.ref))
    else throw new RuntimeException("Ref kind was not TFlt")
  }

  def asDouble: JDouble = {
    if (ref.kind == TDbl) JDouble(ebaReader.doubleTable.readElem(ref.ref))
    else throw new RuntimeException("Ref kind was not TDbl")
  }

  def asBigDec: JBigDec = {
    if (ref.kind == TBigDec) JBigDec(ebaReader.bigDecTable.readElem(ref.ref))
    else throw new RuntimeException("Ref kind was not TBigDec")
  }

  def asString: JString = {
    if (ref.kind == TStr) JString(ebaReader.strTable.readElem(ref.ref))
    else throw new RuntimeException("Ref kind was not TStr")
  }

  def asArray: JArr = {
    if (ref.kind == TArr) JArr(ebaReader.arrTable.readElem(ref.ref))
    else throw new RuntimeException("Ref kind was not TArr")
  }

  def asObject: JObj = {
    if (ref.kind == TObj) JObj(ebaReader.objTable.readElem(ref.ref))
    else throw new RuntimeException("Ref kind was not TObj")
  }

  def asRoot: JRoot = {
    if (ref.kind == TRoot) JRoot(ebaReader.rootTable.readElem(ref.ref))
    else throw new RuntimeException("Ref kind was not TRoot")
  }
}