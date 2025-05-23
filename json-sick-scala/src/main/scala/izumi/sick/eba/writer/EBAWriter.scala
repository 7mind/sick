package izumi.sick.eba.writer

import izumi.sick.eba.writer.codecs.EBACodecs
import izumi.sick.eba.writer.codecs.EBACodecs.{ArrCodec, BigDecimalCodec, BigIntCodec, DoubleCodec, EBAEncoderTable, FixedSizeTableCodec, FloatCodec, IntCodec, LongCodec, ObjCodec, RootCodec, ShortCodec, StringCodec}
import izumi.sick.eba.writer.codecs.util.computeOffsetsFromSizes
import izumi.sick.eba.{EBAStructure, EBATable}
import izumi.sick.model
import izumi.sick.thirdparty.akka.util.ByteString

import java.io.{ByteArrayOutputStream, File, FileOutputStream}
import java.nio.file.{Files, Path}
import scala.collection.immutable.ArraySeq
import scala.reflect.classTag

object EBAWriter {

  final case class EBAInfo(version: Int, headerLen: Int, offsets: Seq[Int], length: Long)

  def writeBytes(structure: EBAStructure, params: model.SICKWriterParameters): (ByteString, EBAInfo) = {
    assert(params.tableWriteStrategy != model.TableWriteStrategy.StreamRepositioning)

    val encoders = new EBACodecs(params)
    val tables = tablesWithEncoders(structure, encoders)
    val tablesSz = tables.length

    object out extends ByteArrayOutputStream() {
      def buf0: Array[Byte] = this.buf
      def length: Int = this.count
    }
    try {
      val version = 0
      val headerLen = (2 + tablesSz) * Integer.BYTES + java.lang.Short.BYTES

      val sizes = new Array[Int](tablesSz)
      var i = 0
      while (i < tablesSz) {
        val table = tables(i)
        val len = table.encoder.writeTable(out, table.table)
        sizes(i) = len.intValue

        i += 1
      }

      val realOffsets = computeOffsetsFromSizes(sizes, headerLen)
      assert(realOffsets.length == tablesSz)

      val output = ByteString.newBuilder
      // header
      IntCodec.encodeTo(version, output)
      IntCodec.encodeTo(tablesSz, output)
      realOffsets.foreach(IntCodec.encodeTo(_, output))
      ShortCodec.encodeTo(structure.settings.objectIndexBucketCount, output)

      // data
      output.addAll(ByteString.ByteString1(out.buf0, 0, out.length))

      (output.result(), EBAInfo(version, headerLen, ArraySeq.unsafeWrapArray(realOffsets), output.length.toLong))
    } finally {
      out.close()
    }
  }

  def writeTempFile(structure: EBAStructure, params: model.SICKWriterParameters): (Path, EBAInfo) = {
    val f = Files.createTempFile("sick", "bin")
    val info = writeFile(structure, f, params)
    (f, info)
  }

  def writeFile(structure: EBAStructure, path: Path, params: model.SICKWriterParameters): EBAInfo = {
    writeFile(structure, path.toFile, params)
  }

  def writeFile(structure: EBAStructure, file: File, params: model.SICKWriterParameters): EBAInfo = {
    val out = new FileOutputStream(file, false)

    val encoders = new EBACodecs(params)
    val tables = tablesWithEncoders(structure, encoders)
    val tablesSz = tables.length

    try {
      val chan = out.getChannel
      chan.truncate(0)

      val version = 0
      val headerLen = (2 + tablesSz) * Integer.BYTES + java.lang.Short.BYTES

      val dummyOffsets = new Array[Int](tablesSz)
      // at this point we don't know dummyOffsets yet, so we write zeros
      val dummyHeader =
        Seq((Seq(version, tablesSz) ++ dummyOffsets).foldLeft(ByteString.empty)(_ ++ IntCodec.encodeSlow(_)))
          ++ Seq(ShortCodec.encodeSlow(structure.settings.objectIndexBucketCount))

      out.write(dummyHeader.foldLeft(ByteString.empty)(_ ++ _).toArrayUnsafe())

      val sizes = new Array[Int](tablesSz)
      var i = 0
      while (i < tablesSz) {
        val table = tables(i)
        val len = table.encoder.writeTable(out, table.table)
        sizes(i) = len.intValue

        i += 1
      }

      // now we know the lengths, so we can write correct dummyOffsets
      val realOffsets = computeOffsetsFromSizes(sizes, headerLen)
      assert(realOffsets.length == tablesSz)

      assert(dummyOffsets.length == sizes.length)
      assert(dummyOffsets.length == tablesSz)

      chan.position(Integer.BYTES * 2)
      out.write(realOffsets.foldLeft(ByteString.empty)(_ ++ IntCodec.encodeSlow(_)).toArrayUnsafe())
      out.flush()

      EBAInfo(version, headerLen, ArraySeq.unsafeWrapArray(realOffsets), file.length())
    } finally {
      if (out != null) out.close()
    }
  }

  private def tablesWithEncoders(structure: EBAStructure, encoders: EBACodecs): Array[EBATableWithEncoder] = {
    import encoders.{FixedSizeArrayTableEncoder, VarSizeTableEncoder}

    val tablesWithEncoders = Array[EBATableWithEncoder](
      EBATableWithEncoder(structure.ints)(using FixedSizeTableCodec(using classTag, IntCodec, implicitly)),
      EBATableWithEncoder(structure.longs)(using FixedSizeTableCodec(using classTag, LongCodec, implicitly)),
      EBATableWithEncoder(structure.bigints)(using VarSizeTableEncoder(using BigIntCodec)),
      EBATableWithEncoder(structure.floats)(using FixedSizeTableCodec(using classTag, FloatCodec, implicitly)),
      EBATableWithEncoder(structure.doubles)(using FixedSizeTableCodec(using classTag, DoubleCodec, implicitly)),
      EBATableWithEncoder(structure.bigDecimals)(using VarSizeTableEncoder(using BigDecimalCodec)),
      EBATableWithEncoder(structure.strings)(using VarSizeTableEncoder(using StringCodec)),
      EBATableWithEncoder(structure.arrs)(using FixedSizeArrayTableEncoder(using ArrCodec)),
      EBATableWithEncoder(structure.objs)(using FixedSizeArrayTableEncoder(using ObjCodec(structure.strings, structure.settings))),
      EBATableWithEncoder(structure.roots)(using FixedSizeTableCodec(using classTag, RootCodec, implicitly)),
    )
    assert(structure.tables.length == tablesWithEncoders.length)

    tablesWithEncoders
  }

  private abstract class EBATableWithEncoder {
    type T
    val table: EBATable[T]
    val encoder: EBAEncoderTable[T]
  }
  private object EBATableWithEncoder {
    def apply[T0](table0: EBATable[T0])(implicit codec0: EBAEncoderTable[T0]): EBATableWithEncoder { type T = T0 } = {
      new EBATableWithEncoder {
        override type T = T0
        override val table: EBATable[T0] = table0
        override val encoder: EBAEncoderTable[T0] = codec0
      }
    }
    def unapply(arg: EBATableWithEncoder): Some[(EBATable[arg.T], EBAEncoderTable[arg.T])] = Some(arg.table, arg.encoder)
  }
}
