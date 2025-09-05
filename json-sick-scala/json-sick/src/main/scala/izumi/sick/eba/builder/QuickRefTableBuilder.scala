package izumi.sick.eba.builder

import izumi.sick.eba.EBATable
import izumi.sick.model.Ref.RefVal

import scala.collection.immutable.ArraySeq
import scala.collection.mutable
import scala.reflect.ClassTag

class QuickRefTableBuilder[V: ClassTag](
  val name: String,
  content: mutable.ListBuffer[V],
  private var count: Int,
) extends GenericRefTableBuilder[V] {

  def insert(v: V): RefVal = {
    val k = RefVal(count)
    content.addOne(v)
    count += 1
    k
  }

  def enumerate(): Map[RefVal, V] = {
    content.iterator.zipWithIndex.map { case (v, k) => (RefVal(k), v) }.toMap
  }

  def isEmpty: Boolean = content.isEmpty

  def size: Int = content.size

  def freeze(): EBATable[V] = new EBATable[V](name, ArraySeq.from(content))

  def rewrite(mapping: V => V): QuickRefTableBuilder[V] = {
    new QuickRefTableBuilder[V](
      name,
      content.map(mapping),
      count,
    )
  }

  override def toString: String = {
    s"""$name:
       |${content.map { case (k, v) => s"$k --> $v" }.mkString("\n")}""".stripMargin
  }
}
