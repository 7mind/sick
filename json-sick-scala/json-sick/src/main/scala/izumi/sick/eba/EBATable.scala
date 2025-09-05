package izumi.sick.eba

import izumi.sick.model.Ref.RefVal

import scala.collection.immutable.ArraySeq

final case class EBATable[+V](
  name: String,
  data: ArraySeq[V],
) {
  @inline def apply(k: RefVal): V = data(k)

  @inline def isEmpty: Boolean = data.isEmpty

  @inline def size: Int = data.size

  @inline def asIterable: Iterable[V] = {
    (0 until size).map(i => data(RefVal(i)))
  }

  @inline def forEach(f: (V, Int) => Unit): Unit = {
    var i = 0
    val sz = data.size
    while (i < sz) {
      f(data(RefVal(i)), i)

      i += 1
    }
  }

  @inline def mapValues[T](f: V => T): EBATable[T] = {
    EBATable(name, data.map(f))
  }

  override def toString: String = {
    s"""$name:
       |${data.zipWithIndex.map { case (v, k) => s"$k=$v" }.mkString("\n")}""".stripMargin
  }
}
