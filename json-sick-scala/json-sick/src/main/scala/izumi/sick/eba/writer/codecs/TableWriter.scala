package izumi.sick.eba.writer.codecs

import izumi.sick.eba.EBATable
import izumi.sick.eba.writer.codecs.EBACodecs.{EBAEncoder, IntCodec}
import izumi.sick.eba.writer.codecs.util.computeOffsetsFromSizes
import izumi.sick.model.TableWriteStrategy
import izumi.sick.thirdparty.akka.util.ByteString

import java.io.OutputStream

private abstract class TableWriter {
  def writeTable[T](stream: OutputStream, table: EBATable[T], codec: EBAEncoder[T]): Long
}

private object TableWriter {

  def apply(arrayWriteStrategy: TableWriteStrategy): TableWriter = {
    arrayWriteStrategy match {
      case TableWriteStrategy.StreamRepositioning => TableWriter.StreamPositioning
      case TableWriteStrategy.SinglePassInMemory => TableWriter.SinglePassInMemory
      case TableWriteStrategy.DoublePass => TableWriter.DoublePass
    }
  }

  object StreamPositioning extends TableWriterStreamRepositioningPlatformSpecific

  object SinglePassInMemory extends TableWriter {
    var counter = 0

    def writeTable[T](stream: OutputStream, table: EBATable[T], codec: EBAEncoder[T]): Long = {
      val bldr = ByteString.newBuilder

      val elemCount = table.size

      var added: Long = 0L

      val sizes = new Array[Int](elemCount)
      val outputs = ByteString.newBuilder
      table.forEach {
        (v, i) =>
          val bsl = codec.encodeTo(v, outputs)
          sizes(i) = bsl
          added += bsl
      }

      val realOffsets = computeOffsetsFromSizes(sizes, 0)
      val lastOffset = realOffsets.lastOption.map(lastOffset => lastOffset + sizes.last).getOrElse(0)

      var headerSz = IntCodec.encodeTo(elemCount, bldr)
      realOffsets.foreach(i => headerSz += IntCodec.encodeTo(i, bldr))
      headerSz += IntCodec.encodeTo(lastOffset, bldr)

      added += headerSz

      bldr.addAll(outputs.result())

      stream.write(bldr.result().toArrayUnsafe())

      added
    }
  }

  object DoublePass extends TableWriter {
    def writeTable[T](stream: OutputStream, table: EBATable[T], codec: EBAEncoder[T]): Long = {
      val elemCount = table.size

      val sizes = new Array[Int](elemCount)
      table.forEach {
        (v, i) =>
          val bsLength = codec.computeSize(v)
          sizes(i) = bsLength
      }

      val realOffsets = computeOffsetsFromSizes(sizes, 0)
      val lastOffset = realOffsets.lastOption.map(lastOffset => lastOffset + sizes.last).getOrElse(0)

      val elemCountBs = IntCodec.encodeSlow(elemCount)
      stream.write(elemCountBs.toArrayUnsafe())
      val realOffsetsBs = realOffsets.map(IntCodec.encodeSlow).foldLeft(ByteString.empty)(_ ++ _)
      stream.write(realOffsetsBs.toArrayUnsafe())
      val lastOffsetAsBytes = IntCodec.encodeSlow(lastOffset)
      stream.write(lastOffsetAsBytes.toArrayUnsafe())

      var added: Long = elemCountBs.length.toLong + realOffsetsBs.length + lastOffsetAsBytes.length

      val outputs = new Array[ByteString](elemCount)
      table.forEach {
        (v, i) =>
          val arr = codec.encodeSlow(v)
          outputs(i) = arr
      }

      outputs.foreach {
        elemBs =>
          stream.write(elemBs.toArrayUnsafe())
          added += elemBs.length
      }

      added
    }
  }
}
