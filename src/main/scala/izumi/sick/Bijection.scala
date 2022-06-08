package izumi.sick

import izumi.sick.Ref.RefVal

import scala.collection.mutable




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

  def get(k: RefVal): Option[V] = data.get(k)
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


