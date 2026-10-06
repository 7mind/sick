package io.izumi.sick

import izumi.sick.eba.reader.IncrementalEBAReader
import org.scalatest.wordspec.AnyWordSpec

import java.io.{BufferedInputStream, ByteArrayInputStream}

class NativeIncrementalReaderTest extends AnyWordSpec {
  "IncrementalEBAReader on Scala Native" should {
    "reject a BufferedInputStream" in {
      val error = intercept[IllegalArgumentException] {
        IncrementalEBAReader.open(new BufferedInputStream(new ByteArrayInputStream(Array.emptyByteArray)), eagerOffsets = false)
      }
      assert(error.getMessage.contains("BufferedInputStream on Scala Native"))
    }
  }
}
