package izumi.sick.indexes

import izumi.sick.indexes.IndexRO.Packed
import izumi.sick.model.{Arr, Obj, Root, ToBytes}
import izumi.sick.tables.RefTableRO
import izumi.sick.thirdparty.akka.util.ByteString

import java.io.FileOutputStream
import java.nio.file.{Files, Path}
import scala.collection.mutable



final class IndexRO(
  val settings: PackSettings,
  val ints: RefTableRO[Int],
  val longs: RefTableRO[Long],
  val bigints: RefTableRO[BigInt],
  val floats: RefTableRO[Float],
  val doubles: RefTableRO[Double],
  val bigDecimals: RefTableRO[BigDecimal],
  val strings: RefTableRO[String],
  val arrs: RefTableRO[Arr],
  val objs: RefTableRO[Obj],
  val roots: RefTableRO[Root],
) {
  def findRoot(str: String): Option[Root] = {
    roots.asSeq.find(r => strings(r.id) == str)
  }

  def summary: String =
    s"""Index summary:
       |${parts.map(_._1).map(p => s"${p.name}: ${p.data.size}").mkString("\n")}""".stripMargin

  override def toString: String = {
    parts.map(_._1).filterNot(_.isEmpty).mkString("\n\n")
  }

  def packFile(): Packed = {
    val f = Files.createTempFile("sick", "bin")
    packFile(f)
  }

  def packFile(f: Path): Packed = {
    try {
      val out = new FileOutputStream(f.toFile, false)

      try {
        val chan = out.getChannel
        chan.truncate(0)
        val version = 0
        val headerLen = (2 + parts.length) * Integer.BYTES + java.lang.Short.BYTES

        val dummyOffsets = new Array[Int](parts.size)
        // at this point we don't know dummyOffsets yet, so we write zeros
        import ToBytes.*
        val header = Seq((Seq(version, parts.length) ++ dummyOffsets).bytes.drop(Integer.BYTES)) ++ Seq(settings.bucketCount.bytes)

        out.write(header.foldLeft(ByteString.empty)(_ ++ _).toArray)

        val sizes = mutable.ArrayBuffer.empty[Int]
        parts.foreach {
          case (p, codec) =>
            val arr = codec.asInstanceOf[ToBytes[Any]].bytes(p.asSeq).toArray
            out.write(arr)
            sizes.append(arr.length)
        }

        // now we know the lengths, so we can write correct dummyOffsets
        val realOffsets = computeOffsetsFromSizes(sizes.toSeq, headerLen)
        assert(dummyOffsets.length == sizes.size)
        assert(dummyOffsets.length == parts.size)
        assert(realOffsets.length == parts.size)
        chan.position(Integer.BYTES * 2)
        out.write(realOffsets.bytes.drop(Integer.BYTES).toArray)

        Packed(version, headerLen, realOffsets, f.toFile.length(), f)
      } finally if (out != null) out.close()
    }

  }

  def parts: Seq[(RefTableRO[Any], ToBytes[Seq[Any]])] = {
    import izumi.sick.model.ToBytes.*
    Seq(
      (ints, implicitly[ToBytes[Seq[Int]]]),
      (longs, implicitly[ToBytes[Seq[Long]]]),
      (bigints, implicitly[ToBytes[Seq[BigInt]]]),
      (floats, implicitly[ToBytes[Seq[Float]]]),
      (doubles, implicitly[ToBytes[Seq[Double]]]),
      (bigDecimals, implicitly[ToBytes[Seq[BigDecimal]]]),
      (strings, implicitly[ToBytes[Seq[String]]]),
      (arrs, implicitly[ToBytes[Seq[Arr]]]),
      (objs, toBytesFixedSizeArray(new ObjToBytes(strings, settings))),
      (roots, implicitly[ToBytes[Seq[Root]]]),
    ).map { case (c, codec) => (c.asInstanceOf[RefTableRO[Any]], codec.asInstanceOf[ToBytes[Seq[Any]]]) }
  }
}

object IndexRO {
  final case class Packed(version: Int, headerLen: Int, offsets: Seq[Int], length: Long, data: Path)
}

case class PackSettings(bucketCount: Short, limit: Short)

object PackSettings {
  def default = PackSettings(128, 2)
}
