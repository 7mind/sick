package izumi.sick.sickcirce

import io.circe.{Json, JsonNumber, JsonObject, UnsafeAccessPrivateJsonNumberSubclasses}
import izumi.sick.eba.EBAStructure
import izumi.sick.eba.builder.EBABuilder
import izumi.sick.model.*
import izumi.sick.model.Ref.RefVal

import scala.collection.mutable

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

    /** @param avoidBigDecimals If `true`, converts `JsonNumber`s to Doubles or Floats in almost all cases. This loses precision, but makes conversion slightly faster. */
    def append(id: String, j: Json, avoidBigDecimals: Boolean): Ref = {
      val idRef = index.addString(id)
      val root = traverse(j)(using avoidBigDecimals)
      index.addRoot(Root(idRef.ref, root))
      root
    }

    @SuppressWarnings(Array("ComparingFloatingPointTypes"))
    private def traverse(j: Json)(implicit avoidBigDecimals: Boolean): Ref = {
      j.foldWith(
        new Json.Folder[Ref] {
          override def onNull: Ref = {
            Ref(RefKind.TNul, RefVal(0))
          }
          override def onBoolean(b: Boolean): Ref = {
            Ref(
              RefKind.TBit,
              if (b) {
                RefVal(1)
              } else {
                RefVal(0)
              },
            )
          }
          override def onNumber(n: JsonNumber): Ref = {
            @inline def addSmallNum(intValue: Int): Ref = {
              if (intValue <= java.lang.Byte.MAX_VALUE && intValue >= java.lang.Byte.MIN_VALUE) {
                Ref(RefKind.TByte, RefVal(intValue))
              } else if (intValue <= java.lang.Short.MAX_VALUE && intValue >= java.lang.Short.MIN_VALUE) {
                Ref(RefKind.TShort, RefVal(intValue))
              } else if (intValue <= java.lang.Integer.MAX_VALUE && intValue >= java.lang.Integer.MIN_VALUE) {
                index.addInt(intValue)
              } else {
                throw new IllegalStateException(s"Cannot decode number $n")
              }
            }

            @inline def addFromBigDecimal(): Ref = {
              n.toBigDecimal match {
                case Some(value) if value.isWhole && value.isValidInt =>
                  val intValue = value.toIntExact
                  addSmallNum(intValue)
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
            }

            if (avoidBigDecimals) {
              n match {
                case jn: UnsafeAccessPrivateJsonNumberSubclasses.JsonLong =>
                  val longValue = jn.value
                  val intValue: Int = longValue.toInt
                  if (longValue == intValue) {
                    addSmallNum(intValue)
                  } else {
                    index.addLong(longValue)
                  }

                case jn: UnsafeAccessPrivateJsonNumberSubclasses.JsonDouble =>
                  index.addDouble(jn.value)

                case jn: UnsafeAccessPrivateJsonNumberSubclasses.JsonFloat =>
                  index.addFloat(jn.value)

                case jn: UnsafeAccessPrivateJsonNumberSubclasses.JsonDecimal =>
                  val db = jn.toDouble
                  if (db == Double.PositiveInfinity || db == Double.NegativeInfinity) {
                    addFromBigDecimal()
                  } else {
                    val flt = db.toFloat
                    if (db == flt) {
                      index.addFloat(flt)
                    } else {
                      index.addDouble(db)
                    }
                  }
                case _ =>
                  addFromBigDecimal()
              }
            } else {
              addFromBigDecimal()
            }
          }
          override def onString(value: String): Ref = {
            index.addString(value)
          }
          override def onArray(arr: Vector[Json]): Ref = {
            index.addArr(
              Arr(
                arr.map(traverse)
              )
            )
          }
          override def onObject(obj: JsonObject): Ref = {
            index.addObj(
              Obj {
                val b = new mutable.HashMap[RefVal, Ref](obj.size, mutable.HashMap.defaultLoadFactor)
                obj.toIterable.foreach {
                  case (k, v) =>
                    val kRef = index.addString(k).ref
                    val vRef = traverse(v)
                    b.put(kRef, vRef)
                }
                b
              }
            )
          }
        }
      )
    }
  }
}
