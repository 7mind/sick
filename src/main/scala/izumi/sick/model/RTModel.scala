package izumi.sick.model

import izumi.sick.model.Ref.RefVal
import izumi.sick.tables.Bijection.RefMappable

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
object Arr {
  implicit object ArrMap extends RefMappable[Arr] {
    override def remap(value: Arr, mapping: Map[Ref, Ref]): Arr = {
      Arr(value.values.map(mapping.apply))
    }
  }
}

case class Obj(values: Vector[(RefVal, Ref)]) {}
object Obj {
  implicit object ObjMap extends RefMappable[Obj] {
    override def remap(value: Obj, mapping: Map[Ref, Ref]): Obj = {
      Obj(value.values.map { case (k, v) => (mapping(Ref(RefKind.TStr, k)).ref, mapping(v)) })
    }
  }
}

case class Root(id: RefVal, ref: Ref)
object Root {
  implicit object RootMap extends RefMappable[Root] {
    override def remap(value: Root, mapping: Map[Ref, Ref]): Root = {
      Root(mapping(Ref(RefKind.TStr, value.id)).ref, mapping(value.ref))
    }
  }
}
