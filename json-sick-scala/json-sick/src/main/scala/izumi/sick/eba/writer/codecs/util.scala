package izumi.sick.eba.writer.codecs

private[writer] object util {

  def computeOffsetsFromSizes(lengths: Array[Int], initial: Int): Array[Int] = {
    val sz = lengths.length
    if (sz == 0) {
      Array.empty
    } else {
      val offsets = new Array[Int](sz)

      offsets(0) = initial
      var i = 1
      while (i < sz) {
        offsets(i) = offsets(i - 1) + lengths(i - 1)
        i += 1
      }
      offsets
    }
  }

  def computeSizesFromOffsets(offsets: collection.IndexedSeq[Int]): Array[Int] = {
    val res = offsets
      .sliding(2)
      .map {
        case collection.Seq(first, second) =>
          second - first
        case _ =>
          -1
      }.filterNot(_ < 0)
      .toArray[Int]
    assert(res.length == (offsets.size - 1) || res.isEmpty)
    res
  }

}
