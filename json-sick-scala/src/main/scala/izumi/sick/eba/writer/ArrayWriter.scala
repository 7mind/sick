package izumi.sick.eba.writer

import izumi.sick.eba.EBATable
import izumi.sick.model.Ref.RefVal
import izumi.sick.thirdparty.akka.util.ByteString

import java.io.{FileOutputStream, OutputStream}
import scala.collection.mutable

trait ArrayWriter {
  def writeArray[T](stream: OutputStream, table: EBATable[T], codec: ToBytes[T], writer: EBAWriter): Long
}

object ArrayWriter {
  object StreamPositioning extends ArrayWriter {
    def writeArray[T](stream: OutputStream, table: EBATable[T], codec: ToBytes[T], writer: EBAWriter): Long = {
      import writer.*

      val chan = stream.asInstanceOf[FileOutputStream].getChannel

      val before = chan.position()

      val dummyOffsets = new Array[RefVal](table.size + 1)
      val header = dummyOffsets.map(_.bytes).foldLeft(table.size.bytes)(_ ++ _)
      val headerArr = header
      stream.write(headerArr.toArray)

      val afterHeader = chan.position()

      val sizes = mutable.ArrayBuffer.empty[RefVal]
      table.forEach {
        v =>
          val arr = codec.bytes(v).toArray
          stream.write(arr)
          sizes.append(arr.length)
      }
      val after = chan.position()

      val realOffsets = computeOffsetsFromSizes(sizes.toSeq, 0)
      val lastOffset = realOffsets.lastOption.map(lastOffset => lastOffset + sizes.last).getOrElse(0)
      chan.position(before)
      val realHeader = realOffsets.map(_.bytes).foldLeft(table.size.bytes)(_ ++ _)
      stream.write(realHeader.toArray)
      stream.write(lastOffset.bytes.toArray)

      assert(afterHeader == chan.position())

      chan.position(after)

      after - before
    }
  }

  object SinglePassInMemory extends ArrayWriter {
    def writeArray[T](stream: OutputStream, table: EBATable[T], codec: ToBytes[T], writer: EBAWriter): Long = {
      import writer.*

      val sizes = mutable.ArrayBuffer.empty[Int]
      val outputs = mutable.ArrayBuffer.empty[ByteString]
      table.forEach {
        v =>
          val arr = codec.bytes(v)
          outputs.append(arr)
          val vlen = arr.length
          sizes.append(vlen)
      }

      val realOffsets = computeOffsetsFromSizes(sizes.toSeq, 0)
      val lastOffset = realOffsets.lastOption.map(lastOffset => lastOffset + sizes.last).getOrElse(0)

      val realHeader = realOffsets.map(_.bytes).foldLeft(table.size.bytes)(_ ++ _)
      stream.write(realHeader.toArray)
      val lastOffsetAsBytes = lastOffset.bytes
      stream.write(lastOffsetAsBytes.toArray)

      var added: Long = realHeader.length + lastOffsetAsBytes.length

      outputs.foreach {
        o =>
          stream.write(o.toArray)
          added += o.length
      }

      added
    }
  }

  object DoublePass extends ArrayWriter {
    def writeArray[T](stream: OutputStream, table: EBATable[T], codec: ToBytes[T], writer: EBAWriter): Long = {
      import writer.*

      val sizes = mutable.ArrayBuffer.empty[Int]
      table.forEach {
        v =>
          val arr = codec.bytes(v)
          val vlen = arr.length
          sizes.append(vlen)
      }

      val realOffsets = computeOffsetsFromSizes(sizes.toSeq, 0)
      val lastOffset = realOffsets.lastOption.map(lastOffset => lastOffset + sizes.last).getOrElse(0)

      val realHeader = realOffsets.map(_.bytes).foldLeft(table.size.bytes)(_ ++ _)
      stream.write(realHeader.toArray)
      val lastOffsetAsBytes = lastOffset.bytes
      stream.write(lastOffsetAsBytes.toArray)

      var added: Long = realHeader.length + lastOffsetAsBytes.length

      val outputs = mutable.ArrayBuffer.empty[ByteString]
      table.forEach {
        v =>
          val arr = codec.bytes(v)
          outputs.append(arr)
      }

      outputs.foreach {
        o =>
          stream.write(o.toArray)
          added += o.length
      }

      added

    }
  }
}
