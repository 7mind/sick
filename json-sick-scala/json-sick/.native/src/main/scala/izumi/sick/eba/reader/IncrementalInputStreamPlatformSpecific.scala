package izumi.sick.eba.reader

import java.io.{ByteArrayInputStream, InputStream}

private[reader] object IncrementalInputStreamPlatformSpecific {
  def requireSupported(inputStream: InputStream): Unit = {
    if (!inputStream.isInstanceOf[ByteArrayInputStream]) {
      throw new IllegalArgumentException(
        s"Cannot read EBA incrementally from $inputStream on Scala Native: only ByteArrayInputStream is supported, because BufferedInputStream returns wrong data after mark/reset once reads go past its initial buffer. Use openBytes or a ByteArrayInputStream instead"
      )
    }
  }
}
