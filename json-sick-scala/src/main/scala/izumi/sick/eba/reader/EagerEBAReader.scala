package izumi.sick.eba.reader

import izumi.sick.eba.writer.codecs.EBACodecs.{ArrCodec, EBADecoderTable, FixedSizeArrayTableDecoder, IntCodec, ObjCodec, ShortCodec}
import izumi.sick.eba.{EBAStructure, EBATable, SICKSettings}
import izumi.sick.model.*

import java.io.{DataInputStream, InputStream}

object EagerEBAReader {

  def readEBAStructure(inputStream: InputStream): EBAStructure = {
    val it = new DataInputStream(inputStream)

    val version = IntCodec.decode(it)
    val expectedVersion = 0
    require(version == expectedVersion, s"SICK version expected to be $expectedVersion, got $version")

    val tableCount = IntCodec.decode(it)
    val expectedTableCount = 10
    require(tableCount == expectedTableCount, s"SICK table count expected to be $expectedTableCount, got $tableCount")

    // skip offsets
    val _ = (0 until tableCount).foreach(_ => IntCodec.decode(it))
    val objectIndexBucketCount = ShortCodec.decode(it)

    val settings = SICKSettings.default.copy(objectIndexBucketCount = objectIndexBucketCount)

    val intTable: EBATable[Int] = EBADecoderTable.readTable[Int](it)
    val longTable: EBATable[Long] = EBADecoderTable.readTable[Long](it)
    val bigIntTable: EBATable[BigInt] = EBADecoderTable.readTable[BigInt](it)

    val floatTable: EBATable[Float] = EBADecoderTable.readTable[Float](it)
    val doubleTable: EBATable[Double] = EBADecoderTable.readTable[Double](it)
    val bigDecTable: EBATable[BigDecimal] = EBADecoderTable.readTable[BigDecimal](it)

    val strTable: EBATable[String] = EBADecoderTable.readTable[String](it)

    val arrTable: EBATable[Arr] = EBADecoderTable.readTable[Arr](it)
    val objTable: EBATable[Obj] = FixedSizeArrayTableDecoder[Obj](using ObjCodec(strTable, settings), implicitly).readTable(it)
    val rootTable: EBATable[Root] = EBADecoderTable.readTable[Root](it)

    EBAStructure(
      intTable,
      longTable,
      bigIntTable,
      floatTable,
      doubleTable,
      bigDecTable,
      strTable,
      arrTable,
      objTable,
      rootTable,
    )(settings)
  }

}
