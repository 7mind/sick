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
  case object TInt extends RefKind
  case object TLng extends RefKind
  case object TArr extends RefKind
  case object TObj extends RefKind
}

case class Ref(kind: RefKind, ref: RefVal) {
  override def toString: String = s"#$ref:$kind"
}
case class Arr(values: Vector[Ref])
case class Obj(values: Vector[(Ref, Ref)])

object Index {
  type RefVal = Long
}
class Index() {
  val strings = mutable.HashMap.empty[RefVal, String]
  val longs = mutable.HashMap.empty[RefVal, Long]
  val doubles = mutable.HashMap.empty[RefVal, Double]
  val arrs = mutable.HashMap.empty[RefVal, Arr]
  val objs = mutable.HashMap.empty[RefVal, Obj]


  override def toString: String = {
    s"""Strings:
       |${strings}
       |
       |Longs:
       |${longs}
       |
       |Doubles
       |${doubles}
       |
       |Arrays:
       |${arrs}
       |
       |Objects:
       |${objs}
       |""".stripMargin
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
        Json.fromLong(longs(ref.ref))
      case RefKind.TLng =>
        Json.fromLong(longs(ref.ref))
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
        println((n.toBigInt, n.toBigDecimal))
        n.toBigInt match {
          case Some(value) =>
            // TODO: handle better
            addLong(value.longValue)
          case None =>
            ???
        }},
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

  def addString(s: String): Ref = {
    val reverse = strings.map { case (k, v) => (v, k)}.toMap
    reverse.get(s) match {
      case Some(value) =>
        Ref(RefKind.TStr, value)
      case None =>
        val idx = strings.size
        strings.put(idx, s)
        Ref(RefKind.TStr, idx)
    }
  }

  def addLong(s: Long): Ref = {
    val reverse = longs.map { case (k, v) => (v, k)}.toMap
    reverse.get(s) match {
      case Some(value) =>
        Ref(RefKind.TLng, value)
      case None =>
        val idx = longs.size
        longs.put(idx, s)
        Ref(RefKind.TLng, idx)
    }
  }

  def addArr(s: Arr): Ref = {
    val reverse = arrs.map { case (k, v) => (v, k)}.toMap
    reverse.get(s) match {
      case Some(value) =>
        Ref(RefKind.TArr, value)
      case None =>
        val idx = arrs.size
        arrs.put(idx, s)
        Ref(RefKind.TArr, idx)
    }
  }

  def addObj(s: Obj): Ref = {
    val reverse = objs.map { case (k, v) => (v, k)}.toMap
    reverse.get(s) match {
      case Some(value) =>
        Ref(RefKind.TObj, value)
      case None =>
        val idx = objs.size
        objs.put(idx, s)
        Ref(RefKind.TObj, idx)
    }
  }
}


object Demo {
    val foo: Seq[Foo] = List(
      Bar(Vector("a")),
      Bar(Vector("a")),
      Bar(Vector("b")),
      Qux(13, Some(14.0)),
      Qux(42, Some(42.0)),
      Qux(42, Some(42.0)),
    )



  def main(args: Array[String]): Unit = {
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

    assert(rec.as[List[Foo]] == Right(foo))
  }
}
