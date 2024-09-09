package izumi.sick.tables

import izumi.sick.model.Ref.RefVal

class EBATable[V](val name: String, val data: Map[RefVal, V]) {
  def apply(k: RefVal): V = data(k)

  def isEmpty: Boolean = data.isEmpty

  def size: RefVal = data.size

  @inline final def asIterable: Iterable[V] = {
    (0 until data.size).map(data)
  }

  @inline final def forEach(f: V => Unit): Unit = {
    asIterable.foreach(f)
  }

  override def toString: String = {
    s"""$name:
       |${data.toSeq.sortBy(_._1).map { case (k, v) => s"$k=$v" }.mkString("\n")}""".stripMargin
  }
}
