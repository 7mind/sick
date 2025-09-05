package izumi.sick.eba.builder

import izumi.sick.eba.EBATable
import izumi.sick.model.Ref.RefVal

import scala.collection.immutable.ArraySeq
import scala.collection.mutable
import scala.reflect.ClassTag

class DeduplicatingRefTableBuilder[V: ClassTag](
  val name: String,
  reverse: mutable.HashMap[V, RefVal],
  private var count: Int,
) extends GenericRefTableBuilder[V] {

  def insert(v: V): RefVal = {
    reverse.get(v) match {
      case Some(value) =>
        value
      case None =>
        val k = RefVal(count)
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

  def freeze(): EBATable[V] = {
    val b = new Array[V](count)
    reverse.foreachEntry {
      (v, i) => b(i) = v
    }
    new EBATable[V](name, ArraySeq.unsafeWrapArray(b))
  }

  def rewrite(mapping: V => V): DeduplicatingRefTableBuilder[V] = {
    new DeduplicatingRefTableBuilder[V](
      name,
      reverse.view.map { case (k, v) => mapping(k) -> v }.to(mutable.HashMap),
      count,
    )
  }

  override def toString: String = {
    s"""$name:
       |${reverse.map { case (v, k) => s"$k --> $v" }.mkString("\n")}""".stripMargin
  }
}
