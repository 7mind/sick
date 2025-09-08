package io.izumi.sick

import typings.node.fsMod as Fs
import typings.node.fsMod.MakeDirectoryOptions
import typings.node.pathMod as Path

import java.io.{ByteArrayInputStream, InputStream}
import scala.scalajs.js.typedarray.{Int8Array, Uint8Array}

abstract class FileOpsPlatformSpecific extends FileOps {

  override def join(first: String, next: String*): String = {
    Path.join((first +: next)*)
  }

  override def walkFiles(path: String, predicate: String => Boolean): List[FileInfo] = {
    val stats = Fs.statSync.apply(path).get
    if (stats.isDirectory()) {
      Fs.readdirSync(path).toList.flatMap(name => walkFiles(Path.join(path, name), predicate))
    } else if (stats.isFile()) {
      List(FileInfo(path, Path.basename(path), stats.size.toLong))
        .filter(predicate `apply` _.path)
    } else {
      Nil
    }
  }

  override def readAllBytes(path: String): Array[Byte] = {
    val buffer = Fs.readFileSync_Buffer(path)
    val uint8Array = buffer.subarray()
    new Int8Array(uint8Array.buffer, uint8Array.byteOffset, uint8Array.length).toArray
  }

  override def writeAllBytes(path: String, bytes: Array[Byte]): Unit = {
    import scala.scalajs.js.typedarray.*
    val int8Array = bytes.toTypedArray
    val uint8Array = new Uint8Array(int8Array.buffer, int8Array.byteOffset, int8Array.length)
    Fs.writeFileSync(path, uint8Array)
  }

  override def delete(path: String): Unit = {
    Fs.unlinkSync(path)
  }

  override def newInputStream(path: String, buffered: Boolean): InputStream = {
    new ByteArrayInputStream(readAllBytes(path))
  }

  override def createDirectories(path: String): Unit = {
    val _ = Fs.mkdirSync(path, MakeDirectoryOptions().setRecursive(true))
  }
}
