package izumi.sick.eba.reader.incremental

import izumi.sick.eba.EBATable
import izumi.sick.eba.reader.incremental.util.{EBACodecFixedOps, asEBATable}
import izumi.sick.eba.writer.codecs.EBACodecs.{DebugTableName, EBACodecFixed, IntCodec}

import java.io.DataInputStream
import scala.reflect.ClassTag

final class IncrementalTableFixed[T: ClassTag] private (
  it: DataInputStream,
  startOffset: Long,
)(implicit
  codec: EBACodecFixed[T],
  debugTableName: DebugTableName[T],
) {
  private final val count = IntCodec.decodeAtOffset(it, startOffset)
  private final val dataOffset = startOffset + IntCodec.blobSize

  def length: Int = count

  def readElem(index: Int): T = {
    assert(index < count, "failed index < count")
    codec.decodeAtOffset(it, dataOffset + index * codec.blobSize)
  }

  // Call this carefully, it may explode!
  // Only use it on small collections and only when you are completely sure that they are actually small
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

object IncrementalTableFixed {
  def allocate[T: ClassTag: EBACodecFixed: DebugTableName](it: DataInputStream, offset: Long) = new IncrementalTableFixed[T](it, offset)
}
