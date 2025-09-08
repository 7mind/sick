package izumi.sick.eba.writer.codecs

import izumi.sick.eba.EBATable
import izumi.sick.eba.writer.codecs.EBACodecs.EBAEncoder

import java.io.OutputStream

private abstract class TableWriterStreamRepositioningPlatformSpecific extends TableWriter {
  final def writeTable[T](stream: OutputStream, table: EBATable[T], codec: EBAEncoder[T]): Long = {
    throw new RuntimeException("Not implemented in JavaScript version")
  }
}
