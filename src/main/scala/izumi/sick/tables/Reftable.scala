package izumi.sick.tables

import izumi.sick.model.Ref.RefVal

class Reftable[V](val name: String, val data: Map[RefVal, V]) {
  def apply(k: RefVal): V = data(k)

  def isEmpty: Boolean = data.isEmpty

  def asSeq: Seq[V] = {
    (0 until data.size).map(data)
  }

  override def toString: String = {
    s"""$name:
       |${data.toSeq.sortBy(_._1).map { case (k, v) => s"$k=$v" }.mkString("\n")}""".stripMargin
  }
}
