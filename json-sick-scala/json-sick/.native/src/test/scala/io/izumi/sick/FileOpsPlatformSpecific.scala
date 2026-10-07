package io.izumi.sick

import java.io.{ByteArrayInputStream, InputStream}

abstract class FileOpsPlatformSpecific extends NioFileOps {
  override def newInputStream(path: String, buffered: Boolean): InputStream = {
    new ByteArrayInputStream(readAllBytes(path))
  }
}
