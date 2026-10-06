package izumi.sick.eba.reader

import java.io.{BufferedInputStream, InputStream}

private[reader] object IncrementalInputStreamPlatformSpecific {
  def requireSupported(inputStream: InputStream): Unit = {
    if (inputStream.isInstanceOf[BufferedInputStream]) {
      throw new IllegalArgumentException(
        s"Cannot read EBA incrementally from a BufferedInputStream on Scala Native: after mark/reset it returns wrong data once reads go past its initial buffer. Use openBytes or a ByteArrayInputStream instead, inputStream=$inputStream"
      )
    }
  }
}
