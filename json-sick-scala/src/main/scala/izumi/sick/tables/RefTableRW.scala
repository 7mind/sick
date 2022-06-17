package izumi.sick.tables

import izumi.sick.model.Ref
import izumi.sick.model.Ref.RefVal

import scala.collection.mutable

class RefTableRW[V] private (
  val name: String,
  reverse: mutable.HashMap[V, RefVal],
  counters: mutable.HashMap[RefVal, Int],
) {

  def add(v: V): RefVal = {
    reverse.get(v) match {
      case Some(value) =>
        counters.put(value, counters(value) + 1)
        value
      case None =>
        val k = reverse.size: RefVal
        reverse.put(v, k)
        counters.put(k, 1)
        k
    }
  }

  def freq(k: RefVal): Int = counters(k)

  def all(): Map[RefVal, (V, Int)] = {
    reverse.map {
      case (v, k) =>
        (k, (v, counters(k)))
    }.toMap
  }

  def revGet(k: V): Option[RefVal] = reverse.get(k)

  def isEmpty: Boolean = reverse.isEmpty

  def size: Int = reverse.size

  def freeze() = new RefTableRO[V](name, reverse.map(_.swap).toMap)

  def rewrite(mapping: V => V): RefTableRW[V] = {
    new RefTableRW[V](
      name,
      reverse.view.map { case (k, v) => mapping(k) -> v }.to(mutable.HashMap),
      counters,
    )
  }

  override def toString: String = {
    s"""$name:
       |${reverse.map { case (v, k) => s"$k --> $v" }.mkString("\n")}""".stripMargin
  }
}

object RefTableRW {
  def apply[V](name: String): RefTableRW[V] = {
    val reverse = mutable.HashMap.empty[V, RefVal]
    val counters = mutable.HashMap.empty[RefVal, Int]
    new RefTableRW[V](name, reverse, counters)
  }

  trait RefMappable[V] {
    def remap(value: V, mapping: Map[Ref, Ref]): V
  }

  implicit class Remap[V: RefMappable](bijection: RefTableRW[V]) {
    def remap(fullMap: Map[Ref, Ref]): RefTableRW[V] = {
      bijection.rewrite(v => implicitly[RefMappable[V]].remap(v, fullMap))
    }
  }

  def fromMonotonic[V](name: String, content: Seq[(Int, V, Int)]): RefTableRW[V] = {
    assert(content.map(_._1) == content.indices)

    val data = content.map { case (k, v, _) => (k, v) }
    val out = new RefTableRW[V](
      name,
      data.map(_.swap).to(mutable.HashMap),
      content.map { case (k, _, freq) => (k, freq) }.to(mutable.HashMap),
    )
    out
  }
}
