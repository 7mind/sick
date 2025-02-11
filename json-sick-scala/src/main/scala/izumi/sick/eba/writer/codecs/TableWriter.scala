package izumi.sick.eba.writer.codecs

import izumi.sick.eba.EBATable
import izumi.sick.eba.writer.codecs.EBACodecs.{EBAEncoder, IntCodec}
import izumi.sick.eba.writer.codecs.util.computeOffsetsFromSizes
import izumi.sick.model.TableWriteStrategy
import izumi.sick.thirdparty.akka.util.ByteString

import java.io.{FileOutputStream, OutputStream}
import scala.collection.mutable

private sealed abstract class TableWriter {
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

  object StreamPositioning extends TableWriter {
    def writeTable[T](stream: OutputStream, table: EBATable[T], codec: EBAEncoder[T]): Long = {
      val chan = stream.asInstanceOf[FileOutputStream].getChannel

      val beforePos = chan.position()

      val elemCount = table.size
      stream.write(IntCodec.encode(elemCount).toArrayUnsafe())

      val beforeOffsetsPos = chan.position()

      val dummyOffsets = new Array[Int](elemCount + 1)
      val dummyHeader = dummyOffsets.foldLeft(ByteString.empty)(_ ++ IntCodec.encode(_))
      stream.write(dummyHeader.toArray)

      val afterHeader = chan.position()

      val sizes = mutable.ArrayBuffer.empty[Int]
      table.forEach {
        v =>
          val bs = codec.encode(v).toArray
          stream.write(bs)
          sizes.append(bs.length)
      }
      val afterPos = chan.position()

      val realOffsets = computeOffsetsFromSizes(sizes, 0)
      val lastOffset = realOffsets.lastOption.map(lastOffset => lastOffset + sizes.last).getOrElse(0)
      chan.position(beforeOffsetsPos)

      val realOffsetsBs = realOffsets.foldLeft(ByteString.empty)(_ ++ IntCodec.encode(_))
      stream.write(realOffsetsBs.toArray)
      stream.write(IntCodec.encode(lastOffset).toArrayUnsafe())

      assert(afterHeader == chan.position())

      chan.position(afterPos)

      afterPos - beforePos
    }
  }

  object SinglePassInMemory extends TableWriter {
    def writeTable[T](stream: OutputStream, table: EBATable[T], codec: EBAEncoder[T]): Long = {
      val sizes = mutable.ArrayBuffer.empty[Int]
      val outputs = mutable.ArrayBuffer.empty[ByteString]
      table.forEach {
        v =>
          val bs = codec.encode(v)
          outputs.append(bs)
          sizes.append(bs.length)
      }

      val realOffsets = computeOffsetsFromSizes(sizes, 0)
      val lastOffset = realOffsets.lastOption.map(lastOffset => lastOffset + sizes.last).getOrElse(0)

      val elemCount = table.size
      val elemCountBs = IntCodec.encode(elemCount)
      stream.write(elemCountBs.toArrayUnsafe())
      val realOffsetsBs = realOffsets.foldLeft(ByteString.empty)(_ ++ IntCodec.encode(_))
      stream.write(realOffsetsBs.toArray)
      val lastOffsetAsBytes = IntCodec.encode(lastOffset)
      stream.write(lastOffsetAsBytes.toArrayUnsafe())

      var added: Long = elemCountBs.length.toLong + realOffsetsBs.length + lastOffsetAsBytes.length

      outputs.foreach {
        elemBs =>
          stream.write(elemBs.toArray)
          added += elemBs.length
      }

      added
    }
  }

  object DoublePass extends TableWriter {
    def writeTable[T](stream: OutputStream, table: EBATable[T], codec: EBAEncoder[T]): Long = {
      val sizes = mutable.ArrayBuffer.empty[Int]
      table.forEach {
        v =>
          val bs = codec.encode(v)
          sizes.append(bs.length)
      }

      val realOffsets = computeOffsetsFromSizes(sizes, 0)
      val lastOffset = realOffsets.lastOption.map(lastOffset => lastOffset + sizes.last).getOrElse(0)

      val elemCount = table.size
      val elemCountBs = IntCodec.encode(elemCount)
      stream.write(elemCountBs.toArrayUnsafe())
      val realOffsetsBs = realOffsets.map(IntCodec.encode).foldLeft(ByteString.empty)(_ ++ _)
      stream.write(realOffsetsBs.toArray)
      val lastOffsetAsBytes = IntCodec.encode(lastOffset)
      stream.write(lastOffsetAsBytes.toArrayUnsafe())

      var added: Long = elemCountBs.length.toLong + realOffsetsBs.length + lastOffsetAsBytes.length

      val outputs = mutable.ArrayBuffer.empty[ByteString]
      table.forEach {
        v =>
          val arr = codec.encode(v)
          outputs.append(arr)
      }

      outputs.foreach {
        elemBs =>
          stream.write(elemBs.toArray)
          added += elemBs.length
      }

      added
    }
  }
}
