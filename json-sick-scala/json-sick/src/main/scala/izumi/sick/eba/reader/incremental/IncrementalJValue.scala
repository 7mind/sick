package izumi.sick.eba.reader.incremental

import izumi.sick.model.{Ref, Root}

sealed trait IncrementalJValue
object IncrementalJValue {
  case object JNul extends IncrementalJValue
  final case class JBit(boolean: Boolean) extends IncrementalJValue

  final case class JByte(byte: Byte) extends IncrementalJValue
  final case class JShort(short: Short) extends IncrementalJValue

  final case class JInt(int: Int) extends IncrementalJValue
  final case class JLong(long: Long) extends IncrementalJValue
  final case class JBigInt(bigInt: BigInt) extends IncrementalJValue

  final case class JFloat(float: Float) extends IncrementalJValue
  final case class JDouble(double: Double) extends IncrementalJValue
  final case class JBigDec(bigDec: BigDecimal) extends IncrementalJValue

  final case class JString(string: String) extends IncrementalJValue

  final case class JArr(arr: IncrementalTableFixed[Ref]) extends IncrementalJValue
  final case class JObj(obj: OneObjTable) extends IncrementalJValue
  final case class JRoot(root: Root) extends IncrementalJValue
}
