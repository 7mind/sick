package izumi.sick


import io.circe._
import io.circe.parser._
import io.circe.syntax._
import izumi.sick.Index.RefVal

import scala.collection.mutable

sealed trait Foo
case class Bar(xs: Vector[String]) extends Foo
case class Qux(i: Long, d: Option[Double]) extends Foo


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

}

case class Ref(kind: RefKind, ref: RefVal) {
  override def toString: String = s"#$ref:$kind"
}
case class Arr(values: Vector[Ref])
case class Obj(values: Vector[(Ref, Ref)])

object Index {
  type RefVal = Long
}


class Bijection[V](val name: String) {
  private val data = mutable.HashMap.empty[RefVal, V]
  private val reverse = mutable.HashMap.empty[V, RefVal]

  def add(v: V): RefVal = {
    reverse.get(v) match {
      case Some(value) =>
        value
      case None =>
        val k = data.size : RefVal
        data.put(k, v)
        reverse.put(v, k)
        k
    }
  }

  def apply(k: RefVal): V = data(k)

  def isEmpty: Boolean = data.isEmpty

  override def toString: String = {

    s"""$name:
       |${data.map { case (k, v) => s"$k --> $v" }.mkString("\n")}""".stripMargin
  }
}

class Index() {
  val strings = new Bijection[String]("Strings")

  val ints = new Bijection[Int]("Integers")
  val longs = new Bijection[Long]("Longs")
  val bigints = new Bijection[BigInt]("Bigints")

  val floats = new Bijection[Float]("Floats")
  val doubles = new Bijection[Double]("Doubles")
  val bigDecimals = new Bijection[BigDecimal]("BigDecs")

  val arrs = new Bijection[Arr]("Arrays")
  val objs = new Bijection[Obj]("Objects")


  override def toString: String = {
    Seq(strings, ints, longs, bigints, floats, doubles, bigDecimals, arrs, objs).filterNot(_.isEmpty).mkString("\n\n")
  }

  def reconstruct(ref: Ref): Json = {
    ref.kind match {
      case RefKind.TNul =>
        Json.Null
      case RefKind.TBit =>
        Json.fromBoolean(ref.ref == 1)
      case RefKind.TStr =>
        Json.fromString(strings(ref.ref))

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

      case RefKind.TArr =>
        val a = arrs(ref.ref)
        Json.fromValues(a.values.map(reconstruct) )
      case RefKind.TObj =>
        val o = objs(ref.ref)
        Json.fromFields(o.values.map {
          case (k, v) =>
            assert(k.kind == RefKind.TStr)
            (strings(k.ref), reconstruct(v))
        } )
    }
  }

  def traverse(j: Json): Ref = {
    j.fold(
      Ref(RefKind.TNul, 0),
      b => Ref(RefKind.TBit, if (b) {1} else {0}),
      n => {
        n.toBigDecimal match {
          case Some(value) if value.isWhole && value.isValidInt  =>
            addInt(value.toIntExact)
          case Some(value) if value.isWhole && value.isValidLong  =>
            addLong(value.toLongExact)
          case Some(value) if value.isWhole  =>
            addBigInt(value.toBigIntExact.getOrElse(???))
          case Some(value) if value.isDecimalFloat  =>
            addFloat(value.floatValue)
          case Some(value) if value.isDecimalDouble  =>
            addDouble(value.doubleValue)
          case Some(value)  =>
            println((value, value.isDecimalDouble, value.isDecimalFloat, value.isBinaryFloat))
            addBigDec(value)
          case None =>
            ???
        }
      },
      s =>  addString(s),
      arr => addArr(Arr(
        arr.map(traverse)
      )),
      obj => addObj(Obj(
        obj.toMap.toVector.map {
          case (k, v) =>
            (addString(k), traverse(v))
        }
      ))
    )
  }

  def addString(s: String): Ref = Ref(RefKind.TStr, strings.add(s))

  def addLong(s: Long): Ref = Ref(RefKind.TLng, longs.add(s))
  def addInt(s: Int): Ref = Ref(RefKind.TInt, ints.add(s))
  def addBigInt(s: BigInt): Ref = Ref(RefKind.TBigInt, bigints.add(s))

  def addFloat(s: Float): Ref = Ref(RefKind.TFlt, floats.add(s))
  def addDouble(s: Double): Ref = Ref(RefKind.TDbl, doubles.add(s))
  def addBigDec(s: BigDecimal): Ref = Ref(RefKind.TBigDec, bigDecimals.add(s))

  def addArr(s: Arr): Ref = Ref(RefKind.TArr, arrs.add(s))

  def addObj(s: Obj): Ref = Ref(RefKind.TObj, objs.add(s))
}


object Demo {
    val foo: Seq[Foo] = List(
      Bar(Vector("a")),
      Bar(Vector("a")),
      Bar(Vector("b")),
      Qux(13, Some(14.0)),
      Qux(42, Some(42.1)),
      Qux(42, Some(42.1)),
    )



  def main(args: Array[String]): Unit = {
//    val n = JsonNumber.fromDecimalStringUnsafe("1000000000000000000000000000000000000000000000000000000000000000000000000000")
//    println(1L << 18)
//    println((n, n.toBigDecimal, n.toBigInt))

    implicit def BarCodec: Codec.AsObject[Bar] = io.circe.generic.semiauto.deriveCodec
    implicit def QuxCodec: Codec.AsObject[Qux] = io.circe.generic.semiauto.deriveCodec
    implicit def FooCodec: Codec.AsObject[Foo] = io.circe.generic.semiauto.deriveCodec

    val json = foo.asJson
    println(json.as[List[Foo]])
    assert(json.as[List[Foo]] == Right(foo))

    val index = new Index()
    val root = index.traverse(json)

    println("ROOT:"+ root)
    println(index)
    val rec = index.reconstruct(root)
    val restored = rec.as[List[Foo]]
    println("RECONSTRUCTED:"+restored)

    assert(restored == Right(foo))
  }
}
