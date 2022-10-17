package izumi.sick.model

import izumi.sick.model.Ref.RefVal
import izumi.sick.tables.RefTableRW.RefMappable

sealed trait RefKind
object RefKind {
  sealed trait NonMappable extends RefKind
  sealed trait Mappable extends RefKind

  case object TNul extends NonMappable
  case object TBit extends NonMappable
  case object TByte extends NonMappable
  case object TShort extends NonMappable

  case object TStr extends Mappable
  case object TArr extends Mappable
  case object TObj extends Mappable

  case object TInt extends Mappable
  case object TLng extends Mappable

  case object TDbl extends Mappable
  case object TFlt extends Mappable
  case object TBigDec extends Mappable
  case object TBigInt extends Mappable

  case object TRoot extends Mappable

  implicit class RefKindExt(kind: RefKind) {
    def index: Byte = kind match {
      case TNul => 0
      case TBit => 1

      case TByte   => 2
      case TShort  => 3
      case TInt    => 4
      case TLng    => 5
      case TBigInt => 6

      case TDbl    => 7
      case TFlt    => 8
      case TBigDec => 9

      case TStr  => 10
      case TArr  => 11
      case TObj  => 12
      case TRoot => 15
    }
  }
}

final case class Ref(kind: RefKind, ref: RefVal)
object Ref {
  type RefVal = Int
}

final case class Arr(values: Vector[Ref])
object Arr {
  implicit object ArrMap extends RefMappable[Arr] {
    override def remap(value: Arr, mapping: Map[Ref, Ref]): Arr = {
      Arr(value.values.map(RefMappable.refRemap(_)(mapping.apply)))
    }
  }
}

final case class Obj(values: Vector[(RefVal, Ref)]) {}
object Obj {
  implicit object ObjMap extends RefMappable[Obj] {
    override def remap(value: Obj, mapping: Map[Ref, Ref]): Obj = {
      Obj(value.values.map { case (k, v) => (mapping(Ref(RefKind.TStr, k)).ref, RefMappable.refRemap(v)(mapping.apply)) })
    }
  }
}

final case class Root(id: RefVal, ref: Ref)
object Root {
  implicit object RootMap extends RefMappable[Root] {
    override def remap(value: Root, mapping: Map[Ref, Ref]): Root = {
      Root(mapping(Ref(RefKind.TStr, value.id)).ref, RefMappable.refRemap(value.ref)(mapping.apply))
    }
  }
}
