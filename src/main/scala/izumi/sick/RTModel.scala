package izumi.sick

import izumi.sick.Ref.RefVal


sealed trait RefKind
object RefKind {
  case object TNul extends RefKind
  case object TBit extends RefKind
  case object TStr extends RefKind
  case object TArr extends RefKind
  case object TObj extends RefKind

  case object TInt extends RefKind
  case object TLng extends RefKind
  case object TDbl extends RefKind
  case object TFlt extends RefKind
  case object TBigDec extends RefKind
  case object TBigInt extends RefKind


  implicit class RefKindExt(kind: RefKind) {
    def index: Int = kind match {
      case TNul => 0
      case TBit => 1
      case TStr => 2
      case TArr => 3
      case TObj => 4
      case TInt => 5
      case TLng => 6
      case TDbl => 7
      case TFlt => 8
      case TBigDec => 9
      case TBigInt => 10
    }
  }
}

case class Ref(kind: RefKind, ref: RefVal) {
  override def toString: String = s"$ref:${kind.index}"
}
object Ref {
  type RefVal = Int

}

case class Arr(values: Vector[Ref]) {
  override def toString: String = values.mkString(";")
}
case class Obj(values: Vector[(RefVal, Ref)]) {
  override def toString: String = values.map(v => s"${v._1},${v._2}").mkString(";")
}
