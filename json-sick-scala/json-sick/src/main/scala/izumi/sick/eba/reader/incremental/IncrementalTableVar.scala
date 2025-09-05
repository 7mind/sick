package izumi.sick.eba.reader.incremental

import izumi.sick.eba.EBATable
import izumi.sick.eba.reader.incremental.util.{EBACodecFixedOps, InputStreamOps, asEBATable}
import izumi.sick.eba.writer.codecs.EBACodecs.{DebugTableName, EBACodecVar, IntCodec}

import java.io.DataInputStream
import scala.collection.immutable.ArraySeq
import scala.reflect.ClassTag

final class IncrementalTableVar[T: ClassTag] private (
  it: DataInputStream,
  offset: Long,
  eagerOffsets: Boolean,
  codec: (DataInputStream, Long, Int) => T,
)(implicit
  debugTableName: DebugTableName[T]
) {
//  private final val startOffset = offset
  private final val count = IntCodec.decodeAtOffset(it, offset)
  private final val sizeOffset = offset + IntCodec.blobSize
  private final val dataOffset = sizeOffset + (count + 1) * IntCodec.blobSize

  private val offsets: ArraySeq[Int] = if (eagerOffsets) {
    ArraySeq.fill(count + 1)(IntCodec.decode(it))
  } else {
    null
  }

  def length: Int = count

  def readElem(index: Int): T = {
    assert(index < count, "failed index < count")

    @inline def getOffset(i: Int) = {
      if (eagerOffsets) {
        offsets(i)
      } else {
        IntCodec.decodeAtOffset(it, sizeOffset + (i * IntCodec.blobSize))
      }
    }

    val relativeDataOffset = getOffset(index)
    val endOffset = getOffset(index + 1)

    val absoluteStartOffset = dataOffset + relativeDataOffset
    val size = endOffset - relativeDataOffset

    it.reset()
    it._skipNBytes(absoluteStartOffset)
    codec(it, absoluteStartOffset, size)
  }

  def readAll(): List[T] = {
    val b = List.newBuilder[T]
    (0 until count).foreach(i => b.addOne(readElem(i)))
    b.result()
  }

  def readAllTable(): EBATable[T] = {
    asEBATable(readAll())
  }

  def iterator: Iterator[T] = {
    Iterator.tabulate(count)(readElem)
  }

  override def toString: String = s"{${debugTableName.tableName} table with $count elements}"
}

object IncrementalTableVar {
  def allocate[T: ClassTag: EBACodecVar: DebugTableName](it: DataInputStream, offset: Long, eagerOffsets: Boolean): IncrementalTableVar[T] = {
    val codec = implicitly[EBACodecVar[T]]
    new IncrementalTableVar[T](it, offset, eagerOffsets, (it, _, size) => codec.decode(it, size))
  }

  def allocateWith[T: ClassTag: DebugTableName](
    it: DataInputStream,
    offset: Long,
    eagerOffsets: Boolean,
  )(f: (DataInputStream, Long, Int) => T
  ): IncrementalTableVar[T] = {
    new IncrementalTableVar[T](it, offset, eagerOffsets, f)
  }
}
