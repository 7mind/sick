package izumi.sick.eba.reader.incremental

import izumi.sick.eba.EBATable
import izumi.sick.eba.writer.codecs.EBACodecs.{DebugTableName, EBACodecFixed}

import java.io.{DataInputStream, EOFException, IOException, InputStream}
import scala.collection.immutable.ArraySeq
import scala.reflect.ClassTag

object util {

  implicit final class EBACodecFixedOps[T](private val codec: EBACodecFixed[T]) extends AnyVal {
    def decodeAtOffset(it: DataInputStream, offset: Long): T = {
      it.reset()
      it._skipNBytes(offset)
      codec.decode(it)
    }
  }

  implicit final class InputStreamOps(private val is: InputStream) extends AnyVal {
    @throws[IOException]
    def _skipNBytes(n0: Long): Unit = {
      var n = n0
      while (n > 0) {
        val ns = is.skip(n)
        if (ns > 0 && ns <= n) {
          // adjust number to skip
          n -= ns
        } else if (ns == 0) { // no bytes skipped
          // read one byte to check for EOS
          if (is.read() == -1) throw new EOFException
          // one byte read so decrement number to skip
          n -= 1
        } else { // skipped negative or too many bytes
          throw new IOException("Unable to skip exactly")
        }
      }
    }
  }

  def readInt32BE(arr: Array[Byte], offset: Int): Int = {
    (arr(offset) & 0xFF) << 24 | (arr(offset + 1) & 0xFF) << 16 | (arr(offset + 2) & 0xFF) << 8 | (arr(offset + 3) & 0xFF) << 0
  }

  def readUInt16BE(arr: Array[Byte], offset: Int): Char = {
    ((arr(offset) & 0xFF) << 8 | (arr(offset + 1) & 0xFF) << 0).toChar
  }

  def asEBATable[T: ClassTag: DebugTableName](elems: IterableOnce[T]): EBATable[T] = {
    EBATable[T](implicitly[DebugTableName[T]].tableName, ArraySeq.from(elems))
  }

}
