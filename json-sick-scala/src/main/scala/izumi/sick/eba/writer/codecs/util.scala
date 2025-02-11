package izumi.sick.eba.writer.codecs

import scala.collection.immutable.ArraySeq

private[writer] object util {

  def computeOffsetsFromSizes(lengths: collection.Seq[Int], initial: Int): Seq[Int] = {
    val out = lengths
      .foldLeft(Vector(initial)) {
        case (offsets, currentSize) =>
          offsets :+ (offsets.last + currentSize)
      }
      .init
    assert(out.size == lengths.size)
    out
  }

  def computeSizesFromOffsets(offsets: collection.Seq[Int]): ArraySeq[Int] = {
    val res = offsets
      .sliding(2)
      .map {
        case collection.Seq(first, second) =>
          second - first
        case _ =>
          -1
      }.filterNot(_ < 0)
      .to(ArraySeq)
    assert(res.size == (offsets.size - 1) || res.isEmpty)
    res
  }

}
