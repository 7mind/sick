package izumi.sick.eba.builder

import izumi.sick.eba.EBATable
import izumi.sick.model.Ref.RefVal

import scala.collection.mutable

class QuickRefTableBuilder[V](
  val name: String,
  content: mutable.HashMap[RefVal, V],
) extends GenericRefTableBuilder[V] {

  private var count = 0

  def insert(v: V): RefVal = {
    val k = RefVal(count)
    content.put(k, v)
    count += 1
    k
  }

  def enumerate(): Map[RefVal, V] = {
    content.toMap
  }

  def isEmpty: Boolean = content.isEmpty

  def size: Int = content.size

  def freeze(): EBATable[V] = new EBATable[V](name, content.toMap)

  def rewrite(mapping: V => V): QuickRefTableBuilder[V] = {
    new QuickRefTableBuilder[V](
      name,
      content.view.map { case (k, v) => k -> mapping(v) }.to(mutable.HashMap),
    )
  }

  override def toString: String = {
    s"""$name:
       |${content.map { case (k, v) => s"$k --> $v" }.mkString("\n")}""".stripMargin
  }
}
