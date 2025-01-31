package izumi.sick.sickcirce

import io.circe.Json
import izumi.sick.eba.EBAStructure
import izumi.sick.eba.builder.EBABuilder
import izumi.sick.model.*
import izumi.sick.model.Ref.RefVal

object CirceTraverser {
  implicit final class ROIndexExt(private val index: EBAStructure) extends AnyVal {

    @SuppressWarnings(Array("OptionGet"))
    def reconstruct(ref: Ref): Json = {
      ref.kind match {
        case RefKind.TNul =>
          Json.Null
        case RefKind.TBit =>
          Json.fromBoolean(ref.ref == 1)

        case RefKind.TByte =>
          Json.fromInt(ref.ref)
        case RefKind.TShort =>
          Json.fromInt(ref.ref)

        case RefKind.TInt =>
          Json.fromInt(index.ints(ref.ref))
        case RefKind.TLng =>
          Json.fromLong(index.longs(ref.ref))
        case RefKind.TBigInt =>
          Json.fromBigInt(index.bigints(ref.ref))

        case RefKind.TFlt =>
          Json.fromFloat(index.floats(ref.ref)).get
        case RefKind.TDbl =>
          Json.fromDouble(index.doubles(ref.ref)).get
        case RefKind.TBigDec =>
          Json.fromBigDecimal(index.bigDecimals(ref.ref))

        case RefKind.TStr =>
          Json.fromString(index.strings(ref.ref))

        case RefKind.TArr =>
          val a = index.arrs(ref.ref)
          Json.fromValues(a.values.map(reconstruct))
        case RefKind.TObj =>
          val o = index.objs(ref.ref)
          Json.fromFields(o.values.map {
            case (k, v) =>
              (index.strings(k), reconstruct(v))
          })
        case RefKind.TRoot =>
          // this shouldn't actually happen
          reconstruct(index.roots(ref.ref).ref)
      }
    }
  }

  implicit final class RWIndexExt(private val index: EBABuilder) extends AnyVal {

    def append(id: String, j: Json): Ref = {
      val idRef = index.addString(id)
      val root = traverse(j)
      index.addRoot(Root(idRef.ref, root))
      root
    }

    private def traverse(j: Json): Ref = {
      j.fold(
        jsonNull = Ref(RefKind.TNul, RefVal(0)),
        jsonBoolean = b =>
          Ref(
            RefKind.TBit,
            if (b) {
              RefVal(1)
            } else {
              RefVal(0)
            },
          ),
        jsonNumber = n => {
          n.toBigDecimal match {
            case Some(value) if value.isWhole && value.isValidInt =>
              val intValue = value.toIntExact
              if (intValue <= java.lang.Byte.MAX_VALUE && intValue >= java.lang.Byte.MIN_VALUE) {
                Ref(RefKind.TByte, RefVal(intValue))
              } else if (intValue <= java.lang.Short.MAX_VALUE && intValue >= java.lang.Short.MIN_VALUE) {
                Ref(RefKind.TShort, RefVal(intValue))
              } else if (intValue <= java.lang.Integer.MAX_VALUE && intValue >= java.lang.Integer.MIN_VALUE) {
                index.addInt(intValue)
              } else {
                throw new IllegalStateException(s"Cannot decode number $n")
              }
            case Some(value) if value.isWhole && value.isValidLong =>
              index.addLong(value.toLongExact)
            case Some(value) if value.isWhole =>
              index.addBigInt(value.toBigIntExact.getOrElse(throw new IllegalStateException(s"Cannot decode BigInt $n")))
            case Some(value) if value.isDecimalFloat =>
              index.addFloat(value.floatValue)
            case Some(value) if value.isDecimalDouble =>
              index.addDouble(value.doubleValue)
            case Some(value) =>
              index.addBigDec(value)
            case None =>
              throw new IllegalStateException(s"Cannot decode number $n")
          }
        },
        jsonString = index.addString,
        jsonArray = arr =>
          index.addArr(
            Arr(
              arr.map(traverse)
            )
          ),
        jsonObject = obj =>
          index.addObj(
            Obj(
              obj.toMap.map {
                case (k, v) =>
                  (index.addString(k).ref, traverse(v))
              }
            )
          ),
      )
    }
  }
}
