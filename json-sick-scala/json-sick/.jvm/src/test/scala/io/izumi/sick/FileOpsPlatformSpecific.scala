package io.izumi.sick

import java.io.{BufferedInputStream, InputStream}
import java.nio.file.{Files, Paths}
import scala.jdk.CollectionConverters.*

abstract class FileOpsPlatformSpecific extends FileOps {

  override def join(first: String, next: String*): String = {
    Paths.get(first, next*).toString
  }

  override def walkFiles(dir: String, predicate: String => Boolean): List[FileInfo] = {
    val path = Paths.get(dir)
    val stream = Files.walk(path)
    try {
      stream
        .map(_.toFile)
        .filter(_.isFile)
        .filter(predicate `apply` _.getName)
        .iterator()
        .asScala
        .map(f => FileInfo(f.getAbsolutePath, f.getName, f.length()))
        .toList
    } finally stream.close()
  }

  override def readAllBytes(path: String): Array[Byte] = {
    Files.readAllBytes(Paths.get(path))
  }

  override def writeAllBytes(path: String, bytes: Array[Byte]): Unit = {
    val _ = Files.write(Paths.get(path), bytes)
  }

  override def delete(path: String): Unit = {
    val _ = Files.delete(Paths.get(path))
  }

  override def newInputStream(path: String, buffered: Boolean): InputStream = {
    val out = Files.newInputStream(Paths.get(path))
    if (buffered) new BufferedInputStream(out) else out
  }

  override def createDirectories(path: String): Unit = {
    val _ = Paths.get(path).toFile.mkdirs()
  }
}
