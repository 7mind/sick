package io.izumi.sick

import izumi.sick.eba.reader.IncrementalEBAReader
import org.scalatest.wordspec.AnyWordSpec

import java.io.{BufferedInputStream, ByteArrayInputStream, DataInputStream}

class NativeIncrementalReaderTest extends AnyWordSpec {
  "IncrementalEBAReader on Scala Native" should {
    "reject a BufferedInputStream" in {
      val error = intercept[IllegalArgumentException] {
        IncrementalEBAReader.open(new BufferedInputStream(new ByteArrayInputStream(Array.emptyByteArray)), eagerOffsets = false)
      }
      assert(error.getMessage.contains("only ByteArrayInputStream is supported"))
    }

    "reject a BufferedInputStream wrapped in another stream" in {
      val wrapped = new DataInputStream(new BufferedInputStream(new ByteArrayInputStream(Array.emptyByteArray)))
      val error = intercept[IllegalArgumentException] {
        IncrementalEBAReader.open(wrapped, eagerOffsets = false)
      }
      assert(error.getMessage.contains("only ByteArrayInputStream is supported"))
    }
  }
}
