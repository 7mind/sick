package izumi.sick

import izumi.sick.Ref.RefVal


sealed trait RefKind
object RefKind {
  case object TNul extends RefKind
  case object TBit extends RefKind
  case object TStr extends RefKind
  case object TArr extends RefKind
  case object TObj extends RefKind

  case object TByte extends RefKind
  case object TShort extends RefKind
  case object TInt extends RefKind
  case object TLng extends RefKind

  case object TDbl extends RefKind
  case object TFlt extends RefKind
  case object TBigDec extends RefKind
  case object TBigInt extends RefKind

  case object TRoot extends RefKind


  implicit class RefKindExt(kind: RefKind) {
    def index: Byte = kind match {
      case TNul => 0
      case TBit => 1

      case TByte => 2
      case TShort => 3
      case TInt => 4
      case TLng => 5
      case TBigInt => 6

      case TDbl => 7
      case TFlt => 8
      case TBigDec => 9


      case TStr => 10
      case TArr => 11
      case TObj => 12
      case TRoot => 15
    }
  }
}

case class Ref(kind: RefKind, ref: RefVal)
object Ref {
  type RefVal = Int

}

case class Arr(values: Vector[Ref])
case class Obj(values: Vector[(RefVal, Ref)])

case class Root(id: RefVal, ref: Ref)

