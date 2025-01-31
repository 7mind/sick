package izumi.sick.eba.reader

import izumi.sick.eba.writer.EBAEncoders
import izumi.sick.eba.writer.EBAEncoders.{EBACodecTable, ObjToBytes}
import izumi.sick.eba.{EBAStructure, EBATable, SICKSettings}
import izumi.sick.model.*
import izumi.sick.thirdparty.akka.util.ByteString

import java.nio.ByteOrder

object EBAReader {

  def readEBAStructure(bytes0: Array[Byte], codecs: EBAEncoders): EBAStructure = {
    val it = ByteString.fromArrayUnsafe(bytes0).iterator

    // skip version
    val _ = it.getInt(ByteOrder.BIG_ENDIAN)
    val tableCount = it.getInt(ByteOrder.BIG_ENDIAN)
    // skip offsets
    val _ = (0 until tableCount).map(_ => it.getInt(ByteOrder.BIG_ENDIAN)).toVector
    val objectIndexBucketCount = it.getShort(ByteOrder.BIG_ENDIAN)

    val settings = SICKSettings.default.copy(objectIndexBucketCount = objectIndexBucketCount)

    import codecs.{toBytesFixedSizeArrayTable, toBytesVarSizeTable}

    val intTable: EBATable[Int] = EBACodecTable.readTable[Int](it)
    val longTable: EBATable[Long] = EBACodecTable.readTable[Long](it)
    val bigIntTable: EBATable[BigInt] = EBACodecTable.readTable[BigInt](it)
    val floatTable: EBATable[Float] = EBACodecTable.readTable[Float](it)
    val doubleTable: EBATable[Double] = EBACodecTable.readTable[Double](it)
    val bigDecTable: EBATable[BigDecimal] = EBACodecTable.readTable[BigDecimal](it)
    val strTable: EBATable[String] = EBACodecTable.readTable[String](it)
    val arrTable: EBATable[Arr] = EBACodecTable.readTable[Arr](it)
    val objTable: EBATable[Obj] = toBytesFixedSizeArrayTable[Obj](using ObjToBytes(strTable, settings), implicitly).readTable(it)
    val rootTable: EBATable[Root] = EBACodecTable.readTable[Root](it)

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
