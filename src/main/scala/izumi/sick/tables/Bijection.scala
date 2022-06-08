package izumi.sick.tables

import izumi.sick.model.{Arr, Ref}
import izumi.sick.model.Ref.RefVal

import scala.collection.mutable

class Bijection[V] private (val name: String, data: mutable.HashMap[RefVal, V], reverse: mutable.HashMap[V, RefVal], counters: mutable.HashMap[RefVal, Int]) {

  def add(v: V): RefVal = {
    reverse.get(v) match {
      case Some(value) =>
        counters.put(value, counters(value) + 1)
        value
      case None =>
        val k = data.size: RefVal
        data.put(k, v)
        reverse.put(v, k)
        counters.put(k, 1)
        k
    }
  }

  def get(k: RefVal): Option[V] = data.get(k)

  def freq(k: RefVal): Int = counters(k)

  def all(): Map[RefVal, (V, Int)] = {
    data.map {
      case (k, v) =>
        (k, (v, counters(k)))
    }.toMap
  }

  def revGet(k: V): Option[RefVal] = reverse.get(k)

  def apply(k: RefVal): V = data(k)

  def isEmpty: Boolean = data.isEmpty

  def size: Int = data.size

  def freeze() = new Reftable[V](name, data.toMap)

  def rewrite(mapping: V => V): Bijection[V] = {
    new Bijection[V](
      name,
      data.view.mapValues(mapping).to(mutable.HashMap),
      reverse.view.map { case (k, v) => mapping(k) -> v }.to(mutable.HashMap),
      counters,
    )
  }

  override def toString: String = {
    s"""$name:
       |${data.map { case (k, v) => s"$k --> $v" }.mkString("\n")}""".stripMargin
  }
}

object Bijection {
  def apply[V](name: String): Bijection[V] = {
    val data = mutable.HashMap.empty[RefVal, V]
    val reverse = mutable.HashMap.empty[V, RefVal]
    val counters = mutable.HashMap.empty[RefVal, Int]
    new Bijection[V](name, data, reverse, counters)
  }

  trait RefMappable[V] {
    def remap(value: V, mapping: Map[Ref, Ref]): V
  }

  implicit class Remap[V: RefMappable](bijection: Bijection[V]) {
    def remap(fullMap: Map[Ref, Ref]): Bijection[V] = {
      bijection.rewrite(v => implicitly[RefMappable[V]].remap(v, fullMap))
    }
  }

  def fromMonotonic[V](name: String, content: Seq[(Int, V, Int)]): Bijection[V] = {
    assert(content.map(_._1) == content.indices)

    val data = content.map { case (k, v, _) => (k, v) }
    val out = new Bijection[V](
      name,
      data.to(mutable.HashMap),
      data.map(_.swap).to(mutable.HashMap),
      content.map { case (k, _, freq) => (k, freq) }.to(mutable.HashMap),
    )
    out
  }
}
