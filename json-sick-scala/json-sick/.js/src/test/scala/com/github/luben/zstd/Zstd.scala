package com.github.luben.zstd

import scala.annotation.unused

object Zstd {
  def compress(bytes: Array[Byte], @unused level: Int): Array[Byte] = bytes
}
