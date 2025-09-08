package io.izumi.sick

// Using manual bindings to avoid CI errors caused by ScalablyTyped. Uncomment "@types/node" and sbt-converter and ScalablyTypedConverterPlugin and its options to use generated bindings.
import io.izumi.sick.manualNodeBindings.{Fs, Path}
import izumi.sick.jsapi.{bytesToUint8Array, uint8ArrayToBytes}
//import typings.node.fsMod as Fs
//import typings.node.fsMod.MakeDirectoryOptions
//import typings.node.pathMod as Path

import java.io.{ByteArrayInputStream, InputStream}
import scala.scalajs.js
import scala.scalajs.js.annotation.JSImport
import scala.scalajs.js.typedarray.{Int8Array, Uint8Array}

object manualNodeBindings {

  @JSImport("fs", JSImport.Namespace)
  @js.native
  object Fs extends js.Any {
    def statSync(path: String): js.UndefOr[Stat] = js.native
    def readdirSync(path: String): js.Array[String] = js.native
    def writeFileSync(path: String, array: Uint8Array): Unit = js.native
    def unlinkSync(path: String): Unit = js.native
    def mkdirSync(path: String, options: js.Object): js.UndefOr[String] = js.native

    def readFileSync(path: String): Buffer = js.native
  }

  @JSImport("path", JSImport.Namespace)
  @js.native
  object Path extends js.Any {
    def join(s: String*): String = js.native
    def basename(s: String): String = js.native
  }

  @js.native
  trait Stat extends js.Object {
    def isDirectory(): Boolean = js.native
    def isFile(): Boolean = js.native

    var size: Double = js.native
  }

  @js.native
  trait Buffer extends js.Object {
    def subarray(): Uint8Array
  }

}

abstract class FileOpsPlatformSpecific extends FileOps {

  override def join(first: String, next: String*): String = {
    Path.join((first +: next)*)
  }

  override def walkFiles(path: String, predicate: String => Boolean): List[FileInfo] = {
    val stats = Fs.statSync(path).get
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
//    val buffer = Fs.readFileSync_Buffer(path)
    val buffer = Fs.readFileSync(path)
    uint8ArrayToBytes(buffer.subarray())
  }

  override def writeAllBytes(path: String, bytes: Array[Byte]): Unit = {
    Fs.writeFileSync(path, bytesToUint8Array(bytes))
  }

  override def delete(path: String): Unit = {
    Fs.unlinkSync(path)
  }

  override def newInputStream(path: String, buffered: Boolean): InputStream = {
    new ByteArrayInputStream(readAllBytes(path))
  }

  override def createDirectories(path: String): Unit = {
//    val _ = Fs.mkdirSync(path, MakeDirectoryOptions().setRecursive(true))
    val _ = Fs.mkdirSync(path, js.Dynamic.literal(recursive = true))
  }
}
