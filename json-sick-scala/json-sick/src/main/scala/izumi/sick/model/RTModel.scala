package izumi.sick.model

import izumi.sick.model.Ref.RefVal

import scala.reflect.ClassTag

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

  implicit final class RefKindExt(private val kind: RefKind) extends AnyVal {
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

  def fromIndex(index: Byte): RefKind = {
    index match {
      case 0 => RefKind.TNul
      case 1 => RefKind.TBit

      case 2 => RefKind.TByte
      case 3 => RefKind.TShort
      case 4 => RefKind.TInt
      case 5 => RefKind.TLng
      case 6 => RefKind.TBigInt

      case 7 => RefKind.TDbl
      case 8 => RefKind.TFlt
      case 9 => RefKind.TBigDec

      case 10 => RefKind.TStr
      case 11 => RefKind.TArr
      case 12 => RefKind.TObj

      case 15 => RefKind.TRoot

      case x => throw new RuntimeException(s"Unknown RefKind index=$x")
    }
  }
}

final case class Ref(kind: RefKind, ref: RefVal)
object Ref {
  type RefVal = RefVal.T
  object RefVal {
    private[Ref] type T <: Int

    def apply(i: Int): RefVal = i.asInstanceOf[RefVal]

    implicit val classTag: ClassTag[RefVal] = implicitly[ClassTag[Int]].asInstanceOf[ClassTag[RefVal]]
  }
}

final case class Arr(values: Vector[Ref])

final case class Obj(values: collection.Map[RefVal, Ref])

final case class Root(id: RefVal, ref: Ref)
