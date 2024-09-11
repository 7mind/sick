package izumi.sick.sickcirce

import io.circe.Json
import izumi.sick.eba.EBAStructure
import izumi.sick.eba.builder.EBABuilder
import izumi.sick.model.*

object CirceTraverser {
  implicit class ROIndexExt(index: EBAStructure) {

    import index.*

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
          Json.fromInt(ints(ref.ref))
        case RefKind.TLng =>
          Json.fromLong(longs(ref.ref))
        case RefKind.TBigInt =>
          Json.fromBigInt(bigints(ref.ref))

        case RefKind.TFlt =>
          Json.fromFloat(floats(ref.ref)).get
        case RefKind.TDbl =>
          Json.fromDouble(doubles(ref.ref)).get
        case RefKind.TBigDec =>
          Json.fromBigDecimal(bigDecimals(ref.ref))

        case RefKind.TStr =>
          Json.fromString(strings(ref.ref))

        case RefKind.TArr =>
          val a = arrs(ref.ref)
          Json.fromValues(a.values.map(reconstruct))
        case RefKind.TObj =>
          val o = objs(ref.ref)
          Json.fromFields(o.values.map {
            case (k, v) =>
              (strings(k), reconstruct(v))
          })
        case RefKind.TRoot =>
          // this shouldn't actually happen
          reconstruct(roots(ref.ref).ref)
      }
    }
  }

  implicit class RWIndexExt(index: EBABuilder) {
    import index.*

    def append(id: String, j: Json): Ref = {
      val idRef = addString(id)
      val root = traverse(j)
      addRoot(Root(idRef.ref, root))
      root
    }

    private def traverse(j: Json): Ref = {
      j.fold(
        Ref(RefKind.TNul, 0),
        b =>
          Ref(
            RefKind.TBit,
            if (b) {
              1
            } else {
              0
            },
          ),
        n => {
          n.toBigDecimal match {
            case Some(value) if value.isWhole && value.isValidInt =>
              val intValue = value.toIntExact
              if (intValue <= java.lang.Byte.MAX_VALUE && intValue >= java.lang.Byte.MIN_VALUE) {
                Ref(RefKind.TByte, intValue)
              } else if (intValue <= java.lang.Short.MAX_VALUE && intValue >= java.lang.Short.MIN_VALUE) {
                Ref(RefKind.TShort, intValue)
              } else if (intValue <= java.lang.Integer.MAX_VALUE && intValue >= java.lang.Integer.MIN_VALUE) {
                addInt(intValue)
              } else {
                throw new IllegalStateException(s"Cannot decode number $n")
              }
            case Some(value) if value.isWhole && value.isValidLong =>
              addLong(value.toLongExact)
            case Some(value) if value.isWhole =>
              addBigInt(value.toBigIntExact.getOrElse(throw new IllegalStateException(s"Cannot decode BigInt $n")))
            case Some(value) if value.isDecimalFloat =>
              addFloat(value.floatValue)
            case Some(value) if value.isDecimalDouble =>
              addDouble(value.doubleValue)
            case Some(value) =>
              addBigDec(value)
            case None =>
              throw new IllegalStateException(s"Cannot decode number $n")
          }
        },
        s => addString(s),
        arr =>
          addArr(
            Arr(
              arr.map(traverse)
            )
          ),
        obj =>
          addObj(
            Obj(
              obj.toMap.toVector.map {
                case (k, v) =>
                  (addString(k).ref, traverse(v))
              }
            )
          ),
      )
    }
  }
}
