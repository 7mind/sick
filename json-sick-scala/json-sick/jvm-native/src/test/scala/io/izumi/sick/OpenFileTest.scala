package io.izumi.sick

import io.circe.Json
import izumi.sick.SICK
import izumi.sick.eba.reader.IncrementalEBAReader
import izumi.sick.eba.writer.EBAWriter
import izumi.sick.model.{SICKWriterParameters, TableWriteStrategy}
import org.scalatest.wordspec.AnyWordSpec

import java.nio.file.Files

class OpenFileTest extends AnyWordSpec {
  private val rootName: String = "sample.json"
  private val streamingThreshold: Long = 0L

  "IncrementalEBAReader.openFile" should {
    "read a file above the in-memory threshold" in {
      val json = Json.obj("a" -> Json.fromInt(1), "b" -> Json.arr(Json.fromString("x"), Json.fromString("y")))
      val eba = SICK.packJson(json, rootName, dedup = true, dedupPrimitives = true, avoidBigDecimals = false)
      val (bytes, _) = EBAWriter.writeBytes(eba.index, SICKWriterParameters(TableWriteStrategy.SinglePassInMemory))
      val file = Files.createTempFile("sick-open-file", ".bin")
      try {
        Files.write(file, bytes.toArrayUnsafe())
        val reader = IncrementalEBAReader.openFile(file, streamingThreshold, eagerOffsets = false)
        try assert(reader.getRoot(rootName).isDefined)
        finally reader.close()
      } finally {
        Files.delete(file)
      }
    }
  }
}
