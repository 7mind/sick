package izumi.sick


import io.circe._
import io.circe.generic.auto._
import io.circe.parser._
import io.circe.syntax._

import scala.collection.mutable

sealed trait Foo
case class Bar(xs: Vector[String]) extends Foo
case class Qux(i: Int, d: Option[Double]) extends Foo


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

case class Ref(kind: RefKind, ref: Long) {
  override def toString: String = s"#$ref:$kind"
}
case class Arr(values: Vector[Ref])
case class Obj(values: Vector[(Ref, Ref)])

class Index() {
  val strings = mutable.HashMap.empty[Long, String]
  val longs = mutable.HashMap.empty[Long, Long]
  val doubles = mutable.HashMap.empty[Long, Double]
  val arrs = mutable.HashMap.empty[Long, Arr]
  val objs = mutable.HashMap.empty[Long, Obj]


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
    val foo = List(
      Qux(13, Some(14.0)),
      Qux(42, Some(42.0)),
      Qux(42, Some(42.0)),
    )

  def traverse(j: Json, index: Index): Ref = {
    j.fold(
      Ref(RefKind.TNul, 0),
      b => Ref(RefKind.TBit, if (b) {1} else {0}),
      n =>
        n.toBigInt match {
        case Some(value) =>
          index.addLong(value.longValue)
        case None =>
          ???
      },
      s =>           index.addString(s),
      arr => index.addArr(Arr(
        arr.map(traverse(_, index))
      )),
      obj => index.addObj(Obj(
        obj.toMap.toVector.map {
          case (k, v) =>
            (index.addString(k), traverse(v, index))
        }
      ))
    )
  }

  def main(args: Array[String]): Unit = {
      val json = foo.asJson

      val index = new Index()
      traverse(json, index)
      println(index)

      val jsons = json.noSpaces
      println(jsons)

      val decodedFoo = decode[List[Foo]](jsons)
      println(decodedFoo)
  }
}
