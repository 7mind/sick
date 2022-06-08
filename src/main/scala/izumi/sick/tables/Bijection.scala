package izumi.sick.tables

import izumi.sick.model.Ref.RefVal

import scala.collection.mutable

class Bijection[V](val name: String) {
  private val data = mutable.HashMap.empty[RefVal, V]
  private val reverse = mutable.HashMap.empty[V, RefVal]
  private val counters = mutable.HashMap.empty[RefVal, Int]

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

  override def toString: String = {
    s"""$name:
       |${data.map { case (k, v) => s"$k --> $v" }.mkString("\n")}""".stripMargin
  }
}
