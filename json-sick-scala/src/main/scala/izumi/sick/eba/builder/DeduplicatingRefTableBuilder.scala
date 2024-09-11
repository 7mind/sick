package izumi.sick.eba.builder

import izumi.sick.eba.EBATable
import izumi.sick.model.Ref.RefVal

import scala.collection.mutable

class DeduplicatingRefTableBuilder[V](
  val name: String,
  reverse: mutable.HashMap[V, RefVal],
) extends GenericRefTableBuilder[V] {

  private var count = 0

  def insert(v: V): RefVal = {
    reverse.get(v) match {
      case Some(value) =>
        value
      case None =>
        val k = count
        reverse.put(v, k)
        count += 1
        k
    }
  }

  def enumerate(): Map[RefVal, V] = {
    reverse.map(_.swap).toMap
  }

  def isEmpty: Boolean = reverse.isEmpty

  def size: Int = reverse.size

  def freeze(): EBATable[V] = new EBATable[V](name, reverse.map(_.swap).toMap)

  def rewrite(mapping: V => V): DeduplicatingRefTableBuilder[V] = {
    new DeduplicatingRefTableBuilder[V](
      name,
      reverse.view.map { case (k, v) => mapping(k) -> v }.to(mutable.HashMap),
    )
  }

  override def toString: String = {
    s"""$name:
       |${reverse.map { case (v, k) => s"$k --> $v" }.mkString("\n")}""".stripMargin
  }
}
