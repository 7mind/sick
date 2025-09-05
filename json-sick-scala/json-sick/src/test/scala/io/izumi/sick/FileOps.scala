package io.izumi.sick

import java.io.InputStream

trait FileOps {
  def join(first: String, next: String*): String
  def walkFiles(path: String, predicate: String => Boolean): List[FileInfo]
  def readAllBytes(path: String): Array[Byte]
  def writeAllBytes(path: String, bytes: Array[Byte]): Unit
  def delete(path: String): Unit
  def newInputStream(path: String, buffered: Boolean): InputStream
  def createDirectories(path: String): Unit
}

object FileOps extends FileOpsPlatformSpecific with FileOps

final case class FileInfo(path: String, name: String, length: Long)
