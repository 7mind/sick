package izumi.sick.indexes

import izumi.sick.indexes.EBAStructure.Packed
import izumi.sick.model.{Arr, Obj, Root, SICKWriterParameters, ToBytes, ToBytesTable}
import izumi.sick.tables.EBATable
import izumi.sick.thirdparty.akka.util.ByteString

import java.io.FileOutputStream
import java.nio.file.{Files, Path}
import scala.collection.mutable

final class EBAStructure(
  val settings: SICKSettings,
  val ints: EBATable[Int],
  val longs: EBATable[Long],
  val bigints: EBATable[BigInt],
  val floats: EBATable[Float],
  val doubles: EBATable[Double],
  val bigDecimals: EBATable[BigDecimal],
  val strings: EBATable[String],
  val arrs: EBATable[Arr],
  val objs: EBATable[Obj],
  val roots: EBATable[Root],
) {
  def findRoot(str: String): Option[Root] = {
    roots.asIterable.find(r => strings(r.id) == str)
  }

  def summary: String =
    s"""Index summary:
       |${parts.map(_._1).map(p => s"${p.name}: ${p.data.size}").mkString("\n")}""".stripMargin

  override def toString: String = {
    parts.map(_._1).filterNot(_.isEmpty).mkString("\n\n")
  }

  def packFile(params: SICKWriterParameters): Packed = {
    val f = Files.createTempFile("sick", "bin")
    packFile(f, params)
  }

  def packFile(f: Path, params: SICKWriterParameters): Packed = {
    val out = new FileOutputStream(f.toFile, false)

    try {
      val chan = out.getChannel
      chan.truncate(0)
      val version = 0
      val headerLen = (2 + parts.length) * Integer.BYTES + java.lang.Short.BYTES

      val dummyOffsets = new Array[Int](parts.size)
      // at this point we don't know dummyOffsets yet, so we write zeros
      import ToBytes.*
      val header = Seq((Seq(version, parts.length) ++ dummyOffsets).bytes.drop(Integer.BYTES)) ++ Seq(settings.objectIndexBucketCount.bytes)

      out.write(header.foldLeft(ByteString.empty)(_ ++ _).toArray)

      val sizes = mutable.ArrayBuffer.empty[Int]
      parts.foreach {
        case (p, codec) =>
          val len = codec.asInstanceOf[ToBytesTable[Any]].write(out, p, params)
          sizes.append(len.intValue)
      }

      // now we know the lengths, so we can write correct dummyOffsets
      val realOffsets = computeOffsetsFromSizes(sizes.toSeq, headerLen)
      assert(dummyOffsets.length == sizes.size)
      assert(dummyOffsets.length == parts.size)
      assert(realOffsets.length == parts.size)
      chan.position(Integer.BYTES * 2)
      out.write(realOffsets.bytes.drop(Integer.BYTES).toArray)

      Packed(version, headerLen, realOffsets, f.toFile.length(), f)
    } finally {
      if (out != null) out.close()
    }
  }

  def parts: Seq[(EBATable[Any], ToBytesTable[Seq[Any]])] = {
    import izumi.sick.model.ToBytes.*
    Seq(
      (ints, implicitly[ToBytesTable[Int]]),
      (longs, implicitly[ToBytesTable[Long]]),
      (bigints, implicitly[ToBytesTable[BigInt]]),
      (floats, implicitly[ToBytesTable[Float]]),
      (doubles, implicitly[ToBytesTable[Double]]),
      (bigDecimals, implicitly[ToBytesTable[BigDecimal]]),
      (strings, implicitly[ToBytesTable[String]]),
      (arrs, implicitly[ToBytesTable[Arr]]),
      (objs, toBytesFixedSizeArray(new ObjToBytes(strings, settings))),
      (roots, implicitly[ToBytesTable[Root]]),
    ).map { case (c, codec) => (c.asInstanceOf[EBATable[Any]], codec.asInstanceOf[ToBytesTable[Seq[Any]]]) }
  }
}

object EBAStructure {
  final case class Packed(version: Int, headerLen: Int, offsets: Seq[Int], length: Long, data: Path)
}

final case class SICKSettings(
  objectIndexBucketCount: Short,
  minObjectKeysBeforeIndexing: Short,
)

object SICKSettings {
  def default: SICKSettings = SICKSettings(128, 2)
}
