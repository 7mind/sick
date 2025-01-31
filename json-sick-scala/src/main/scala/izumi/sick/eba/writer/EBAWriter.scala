package izumi.sick.eba.writer

import izumi.sick.eba.writer.EBAEncoders.{ArrToBytes, IntToBytes, ObjToBytes, ShortToBytes, EBACodecTable}
import izumi.sick.eba.writer.util.computeOffsetsFromSizes
import izumi.sick.eba.{EBAStructure, EBATable}
import izumi.sick.model.*
import izumi.sick.thirdparty.akka.util.ByteString

import java.io.{ByteArrayOutputStream, File, FileOutputStream}
import java.nio.file.{Files, Path}
import scala.collection.immutable.ArraySeq
import scala.collection.mutable

object EBAWriter {

  def writeBytes(structure: EBAStructure, params: SICKWriterParameters): (ByteString, EBAInfo) = {
    assert(params.tableWriteStrategy != TableWriteStrategy.StreamRepositioning)

    val out = new ByteArrayOutputStream()
    val encoders = new EBAEncoders(params)
    val tables = tablesWithEncoders(structure, encoders)

    try {
      val version = 0
      val headerLen = (2 + tables.length) * Integer.BYTES + java.lang.Short.BYTES

      val sizes = mutable.ArrayBuffer.empty[Int]
      tables.foreach {
        case EBATableWithEncoder(p, encoder) =>
          val len = encoder.writeTable(out, p)
          sizes.append(len.intValue)
      }

      val realOffsets = computeOffsetsFromSizes(sizes, headerLen)
      assert(realOffsets.length == tables.size)

      val header = Seq((Seq(version, tables.length) ++ realOffsets).foldLeft(ByteString.empty)(_ ++ IntToBytes.encode(_)))
        ++ Seq(ShortToBytes.encode(structure.settings.objectIndexBucketCount))

      val data = out.toByteArray
      val headerBytes = header.foldLeft(ByteString.empty)(_ ++ _)

      val output = ByteString(headerBytes.toArray) ++ ByteString(data)
      (output, EBAInfo(version, headerLen, realOffsets, output.length.toLong))
    } finally {
      if (out != null) out.close()
    }
  }

  def writeTempFile(structure: EBAStructure, params: SICKWriterParameters): (Path, EBAInfo) = {
    val f = Files.createTempFile("sick", "bin")
    val info = writeFile(structure, f, params)
    (f, info)
  }

  def writeFile(structure: EBAStructure, path: Path, params: SICKWriterParameters): EBAInfo = {
    writeFile(structure, path.toFile, params)
  }

  def writeFile(structure: EBAStructure, file: File, params: SICKWriterParameters): EBAInfo = {
    val out = new FileOutputStream(file, false)

    val encoders = new EBAEncoders(params)
    val tables = tablesWithEncoders(structure, encoders)

    try {
      val chan = out.getChannel
      chan.truncate(0)

      val version = 0
      val headerLen = (2 + tables.length) * Integer.BYTES + java.lang.Short.BYTES

      val dummyOffsets = new Array[Int](tables.size)
      // at this point we don't know dummyOffsets yet, so we write zeros
      val dummyHeader =
        Seq((Seq(version, tables.length) ++ dummyOffsets).foldLeft(ByteString.empty)(_ ++ IntToBytes.encode(_)))
          ++ Seq(ShortToBytes.encode(structure.settings.objectIndexBucketCount))

      out.write(dummyHeader.foldLeft(ByteString.empty)(_ ++ _).toArray)

      val sizes = mutable.ArrayBuffer.empty[Int]
      tables.foreach {
        case EBATableWithEncoder(p, encoder) =>
          val len = encoder.writeTable(out, p)
          sizes.append(len.intValue)
      }

      // now we know the lengths, so we can write correct dummyOffsets
      val realOffsets = computeOffsetsFromSizes(sizes, headerLen)
      assert(realOffsets.length == tables.size)

      assert(dummyOffsets.length == sizes.size)
      assert(dummyOffsets.length == tables.size)

      chan.position(Integer.BYTES * 2)
      out.write(realOffsets.foldLeft(ByteString.empty)(_ ++ IntToBytes.encode(_)).toArray)
      out.flush()

      EBAInfo(version, headerLen, realOffsets, file.length())
    } finally {
      if (out != null) out.close()
    }
  }

  private def tablesWithEncoders(structure: EBAStructure, encoders: EBAEncoders): ArraySeq[EBATableWithEncoder] = {
    import encoders.{toBytesFixedSizeArrayTable, toBytesVarSizeTable}

    val tablesWithEncoders = ArraySeq(
      EBATableWithEncoder(structure.ints),
      EBATableWithEncoder(structure.longs),
      EBATableWithEncoder(structure.bigints),
      EBATableWithEncoder(structure.floats),
      EBATableWithEncoder(structure.doubles),
      EBATableWithEncoder(structure.bigDecimals),
      EBATableWithEncoder(structure.strings),
      EBATableWithEncoder(structure.arrs),
      EBATableWithEncoder(structure.objs)(toBytesFixedSizeArrayTable(using ObjToBytes(structure.strings, structure.settings), implicitly)),
      EBATableWithEncoder(structure.roots),
    )
    assert(structure.tables.length == tablesWithEncoders.length)

    tablesWithEncoders
  }

  private abstract class EBATableWithEncoder {
    type T
    val table: EBATable[T]
    val codec: EBACodecTable[T]
  }
  private object EBATableWithEncoder {
    def apply[T0](table0: EBATable[T0])(implicit codec0: EBACodecTable[T0]): EBATableWithEncoder { type T = T0 } = new EBATableWithEncoder {
      override type T = T0
      override val table: EBATable[T0] = table0
      override val codec: EBACodecTable[T0] = codec0
    }
    def unapply(arg: EBATableWithEncoder): Some[(EBATable[arg.T], EBACodecTable[arg.T])] = Some(arg.table, arg.codec)
  }
}
