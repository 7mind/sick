package izumi.sick.eba.reader

import java.io.InputStream

private[reader] object IncrementalInputStreamPlatformSpecific {
  def readsFileIntoMemory(fileSize: Long, inMemoryThreshold: Long): Boolean = {
    fileSize <= inMemoryThreshold
  }

  def requireSupported(inputStream: InputStream): Unit = {
    val _ = inputStream
  }
}
