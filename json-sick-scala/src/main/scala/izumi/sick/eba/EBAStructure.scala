package izumi.sick.eba

import izumi.sick.eba.EBAStructure.{PackedBytes, PackedFile}
import izumi.sick.eba.writer.{EBAWriter, ToBytesTable}
import izumi.sick.model.{Arr, ArrayWriteStrategy, Obj, Root, SICKWriterParameters}
import izumi.sick.thirdparty.akka.util.ByteString

import java.io.{ByteArrayOutputStream, FileOutputStream}
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
       |${components.map(p => s"${p.name}: ${p.data.size}").mkString("\n")}""".stripMargin

  override def toString: String = {
    components.filterNot(_.isEmpty).mkString("\n\n")
  }

  def packBytes(params: SICKWriterParameters): PackedBytes = {
    assert(params.arrayWriteStrategy != ArrayWriteStrategy.StreamRepositioning)

    val out = new ByteArrayOutputStream()
    val codec = new EBAWriter(params)
    val tables = parts(codec)

    try {
      val sizes = mutable.ArrayBuffer.empty[Int]
      tables.foreach {
        case (p, codec) =>
          val len = codec.asInstanceOf[ToBytesTable[Any]].write(out, p)
          sizes.append(len.intValue)
      }

      val version = 0
      val headerLen = (2 + tables.length) * Integer.BYTES + java.lang.Short.BYTES

      import codec.*

      val realOffsets = computeOffsetsFromSizes(sizes.toSeq, headerLen)
      assert(realOffsets.length == tables.size)

      val header = Seq((Seq(version, tables.length) ++ realOffsets).bytes.drop(Integer.BYTES)) ++ Seq(settings.objectIndexBucketCount.bytes)

      val data = out.toByteArray
      val headerBytes = header.foldLeft(ByteString.empty)(_ ++ _)

      val output = ByteString(headerBytes.toArray) ++ ByteString(data)
      PackedBytes(version, headerLen, realOffsets, output.length, output)
    } finally {
      if (out != null) out.close()
    }
  }

  def packFile(params: SICKWriterParameters): PackedFile = {
    val f = Files.createTempFile("sick", "bin")
    packFile(f, params)
  }

  def packFile(f: Path, params: SICKWriterParameters): PackedFile = {
    val out = new FileOutputStream(f.toFile, false)

    val codec = new EBAWriter(params)
    val tables = parts(codec)

    try {
      val chan = out.getChannel
      chan.truncate(0)

      val version = 0
      val headerLen = (2 + tables.length) * Integer.BYTES + java.lang.Short.BYTES

      val dummyOffsets = new Array[Int](tables.size)
      // at this point we don't know dummyOffsets yet, so we write zeros
      import codec.*
      val header = Seq((Seq(version, tables.length) ++ dummyOffsets).bytes.drop(Integer.BYTES)) ++ Seq(settings.objectIndexBucketCount.bytes)

      out.write(header.foldLeft(ByteString.empty)(_ ++ _).toArray)

      val sizes = mutable.ArrayBuffer.empty[Int]
      tables.foreach {
        case (p, codec) =>
          val len = codec.asInstanceOf[ToBytesTable[Any]].write(out, p)
          sizes.append(len.intValue)
      }

      // now we know the lengths, so we can write correct dummyOffsets
      val realOffsets = computeOffsetsFromSizes(sizes.toSeq, headerLen)
      assert(dummyOffsets.length == sizes.size)
      assert(dummyOffsets.length == tables.size)
      assert(realOffsets.length == tables.size)
      chan.position(Integer.BYTES * 2)
      out.write(realOffsets.bytes.drop(Integer.BYTES).toArray)

      PackedFile(version, headerLen, realOffsets, f.toFile.length(), f)
    } finally {
      if (out != null) out.close()
    }
  }

  val components: Seq[EBATable[Any]] = Seq(
    ints,
    longs,
    bigints,
    floats,
    doubles,
    bigDecimals,
    strings,
    arrs,
    objs,
    roots,
  ).map(_.asInstanceOf[EBATable[Any]])

  def parts(codec: EBAWriter): Seq[(EBATable[Any], ToBytesTable[Seq[Any]])] = {
    import codec.*
    val codecs = Seq(
      implicitly[ToBytesTable[Int]],
      implicitly[ToBytesTable[Long]],
      implicitly[ToBytesTable[BigInt]],
      implicitly[ToBytesTable[Float]],
      implicitly[ToBytesTable[Double]],
      implicitly[ToBytesTable[BigDecimal]],
      implicitly[ToBytesTable[String]],
      implicitly[ToBytesTable[Arr]],
      toBytesFixedSizeArray(new ObjToBytes(strings, settings)),
      implicitly[ToBytesTable[Root]],
    )
    assert(components.length == codecs.length)

    components
      .zip(codecs).map { case (c, codec) => (c, codec.asInstanceOf[ToBytesTable[Seq[Any]]]) }
  }
}

object EBAStructure {
  final case class PackedFile(version: Int, headerLen: Int, offsets: Seq[Int], length: Long, data: Path)
  final case class PackedBytes(version: Int, headerLen: Int, offsets: Seq[Int], length: Long, data: ByteString)
}

final case class SICKSettings(
  objectIndexBucketCount: Short,
  minObjectKeysBeforeIndexing: Short,
)

object SICKSettings {
  def default: SICKSettings = SICKSettings(128, 2)
}
