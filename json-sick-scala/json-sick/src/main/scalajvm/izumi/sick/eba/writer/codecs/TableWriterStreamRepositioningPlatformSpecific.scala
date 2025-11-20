package izumi.sick.eba.writer.codecs

import izumi.sick.eba.EBATable
import izumi.sick.eba.writer.codecs.EBACodecs.{EBAEncoder, IntCodec}
import izumi.sick.eba.writer.codecs.util.computeOffsetsFromSizes
import izumi.sick.thirdparty.akka.util.ByteString

import java.io.{FileOutputStream, OutputStream}

private abstract class TableWriterStreamRepositioningPlatformSpecific extends TableWriter {
  final def writeTable[T](stream: OutputStream, table: EBATable[T], codec: EBAEncoder[T]): Long = {
    val chan = stream.asInstanceOf[FileOutputStream].getChannel

    val beforePos = chan.position()

    val elemCount = table.size
    stream.write(IntCodec.encodeSlow(elemCount).toArrayUnsafe())

    val beforeOffsetsPos = chan.position()

    val dummyOffsets = new Array[Int](elemCount + 1)
    val dummyHeader = dummyOffsets.foldLeft(ByteString.empty)(_ ++ IntCodec.encodeSlow(_))
    stream.write(dummyHeader.toArrayUnsafe())

    val afterHeader = chan.position()

    val sizes = new Array[Int](table.size)
    table.forEach {
      (v, i) =>
        val bs = codec.encodeSlow(v).toArrayUnsafe()
        stream.write(bs)
        sizes(i) = bs.length
    }
    val afterPos = chan.position()

    val realOffsets = computeOffsetsFromSizes(sizes, 0)
    val lastOffset = realOffsets.lastOption.map(lastOffset => lastOffset + sizes.last).getOrElse(0)
    chan.position(beforeOffsetsPos)

    val realOffsetsBs = realOffsets.foldLeft(ByteString.empty)(_ ++ IntCodec.encodeSlow(_))
    stream.write(realOffsetsBs.toArrayUnsafe())
    stream.write(IntCodec.encodeSlow(lastOffset).toArrayUnsafe())

    assert(afterHeader == chan.position())

    chan.position(afterPos)
    val added = afterPos - beforePos
    added
  }
}
