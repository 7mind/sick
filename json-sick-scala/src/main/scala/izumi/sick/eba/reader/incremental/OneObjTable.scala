package izumi.sick.eba.reader.incremental

import izumi.sick.eba.reader.incremental.util.{EBACodecFixedOps, InputStreamOps, readUInt16BE}
import izumi.sick.eba.writer.codecs.EBACodecs.{CharCodec, IntCodec, ObjConstants, ObjectEntryCodec}
import izumi.sick.model.Ref.RefVal
import izumi.sick.model.{Obj, Ref, RefKind}
import izumi.sick.tools.CBFHash

import java.io.DataInputStream

final class OneObjTable private (
  it: DataInputStream,
  startOffset: Long,
  strings: IncrementalTableVar[String],
  bucketCount: Short,
) {
  private val bucketSize: Long = ObjConstants.bucketSize(bucketCount)
  private var count: Int = _
  private var dataOffset: Long = _

  private val rawIndex: Array[Byte] = {
    val header = CharCodec.decodeAtOffset(it, startOffset)
    if (header == ObjConstants.noIndexMarker) {
      count = IntCodec.decode(it)
      dataOffset = startOffset + CharCodec.blobSize + IntCodec.blobSize
      null
    } else {
      val indexSize = bucketCount * CharCodec.blobSize
      val indexPlusCountSize = indexSize + IntCodec.blobSize
      val rawIndex = new Array[Byte](indexPlusCountSize)
      it.reset()
      it._skipNBytes(startOffset)
      it.readFully(rawIndex, 0, indexPlusCountSize)
      count = util.readInt32BE(rawIndex, indexSize)
      dataOffset = startOffset + indexPlusCountSize
      rawIndex
    }
  }
  assert(count >= 0, "negative OneObj count")

  def length: Int = count

  def readObjectFieldRef(field: String): Ref = {
    var lower = 0
    var upper = count

    if (rawIndex ne null) {
      val hash = CBFHash.compute(field)
      val bucket = (hash / bucketSize).toInt
      val probablyLower = bucketValue(bucket)

      if (probablyLower == ObjConstants.maxIndex) {
        throw new IllegalStateException(
          s"Field $field not found in object $this"
        )
      }

      if (probablyLower >= count) {
        throw new IllegalStateException(
          s"Field $field in object $this produced bucket index ${probablyLower.toInt} which is more than object size $count"
        )
      }

      lower = probablyLower.toInt

      // with optimized index there should be no maxIndex elements in the index and we expect to make exactly ONE iteration
      locally {
        var i = bucket + 1
        while (i < bucketCount) {
          val probablyUpper = bucketValue(i)

          if (probablyUpper <= count) {
            upper = probablyUpper.toInt
            i = bucketCount.toInt
          }

          if (probablyUpper == ObjConstants.maxIndex) {}
          else if (probablyUpper > count) {
            throw new IllegalStateException(
              s"Field $field in object $this produced bucket index ${probablyUpper.toInt} which is more than object size $count"
            )
          }

          i += 1
        }
      }
    }

    assert(lower <= upper, "failed lower <= upper")
    locally {
      var i = lower
      while (i < upper) {
        val k = readKeyOnly(i)
        if (k._1 == field) {
          val kind: RefKind = RefKind.fromIndex(k._2(IntCodec.blobSize))
          val value = RefVal(util.readInt32BE(k._2, IntCodec.blobSize + 1))
          return new Ref(kind, value)
        }
        i += 1
      }
    }

    throw new IllegalStateException(
      s"Field $field not found in object $this"
    )
  }

  def readElem(index: Int): (RefVal, Ref) = {
    assert(index < count, "failed index < count")
    ObjectEntryCodec.decodeAtOffset(it, dataOffset + index * ObjectEntryCodec.blobSize)
  }

  def readAll(): List[(RefVal, Ref)] = {
    val b = List.newBuilder[(RefVal, Ref)]
    (0 until count).foreach(i => b.addOne(readElem(i)))
    b.result()
  }

  def readAllObj(): Obj = {
    Obj(readAll().toMap)
  }

  def iterator: Iterator[(String, Ref)] = {
    Iterator.tabulate(count)(readKey)
  }

  def readKey(index: Int): (String, Ref) = {
    val (kval, ref) = readElem(index)
    (strings.readElem(kval), ref)
  }

  def readKeyOnly(index: Int): (String, Array[Byte]) = {
    it.reset()
    it._skipNBytes(dataOffset + (index * ObjectEntryCodec.blobSize))
    val bytes = new Array[Byte](ObjectEntryCodec.blobSize)
    it.readFully(bytes, 0, ObjectEntryCodec.blobSize)
    val key = strings.readElem(util.readInt32BE(bytes, 0))
    (key, bytes)
  }

  private def bucketValue(bucket: Int): Char = {
    readUInt16BE(rawIndex, bucket * CharCodec.blobSize)
  }

  override def toString: String = s"{OneObj table with $count elements}"
}

object OneObjTable {
  def allocate(it: DataInputStream, startOffset: Long, strings: IncrementalTableVar[String], bucketCount: Short): OneObjTable = {
    new OneObjTable(it, startOffset, strings, bucketCount)
  }
}
