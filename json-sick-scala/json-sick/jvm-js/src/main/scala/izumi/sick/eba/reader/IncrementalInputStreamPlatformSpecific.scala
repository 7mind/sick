package izumi.sick.eba.reader

import java.io.InputStream

private[reader] object IncrementalInputStreamPlatformSpecific {
  def requireSupported(inputStream: InputStream): Unit = {
    val _ = inputStream
  }
}
