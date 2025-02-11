package izumi.sick.eba

import izumi.sick.model.Ref.RefVal

final case class EBATable[+V](
  name: String,
  data: Map[RefVal, V],
) {
  @inline def apply(k: RefVal): V = data(k)

  @inline def isEmpty: Boolean = data.isEmpty

  @inline def size: Int = data.size

  @inline def asIterable: Iterable[V] = {
    (0 until size).map(i => data(RefVal(i)))
  }

  @inline def forEach(f: V => Unit): Unit = {
    asIterable.foreach(f)
  }

  @inline def mapValues[T](f: V => T): EBATable[T] = {
    EBATable(name, data.view.mapValues(f).toMap)
  }

  override def toString: String = {
    s"""$name:
       |${data.toSeq.sortBy(_._1).map { case (k, v) => s"$k=$v" }.mkString("\n")}""".stripMargin
  }
}
