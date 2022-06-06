package izumi.sick


import akka.util.ByteString
import com.github.luben.zstd.Zstd
import io.circe._
import izumi.sick.Ref.RefVal

import java.nio.ByteBuffer
import java.nio.charset.StandardCharsets
import java.nio.file.{Files, Paths}
import scala.collection.mutable

sealed trait Foo
case class Bar(xs: Vector[String]) extends Foo
case class Qux(i: Long, d: Option[Double]) extends Foo







//case class SICKHeader(
//    version: Int,
//    count: Int,
//    offsets: Vector[Int]
//                )
//
//
//
//case class SICKEntry(name: String)
//
//case class SICKBlob(value: ByteString)
//case class SICKEntryBlob(value: ByteString)
//
//class SICKSchema(meta: List[SICKEntry]) {
//  def format(entries: Vector[SICKEntryBlob]): SICKBlob = {
//    assert(entries.size == meta.size)
//      entries.foldLeft((0, List.empty[Int])) {
//        case ((currentOffset, offsets), entry) =>
//          val newOffset = currentOffset + entry.value.length
//          (newOffset, offsets :+ newOffset)
//      }
//
//
//
//    val headerISize = Integer.BYTES * (2 + entries.size)
//
//      val header =
//  }
//}


object Demo {
    val foo: Seq[Foo] = List(
      Bar(Vector("a")),
      Bar(Vector("a")),
      Bar(Vector("b")),
      Qux(13, Some(14.0)),
      Qux(42, Some(42.1)),
      Qux(42, Some(42.1)),
    )



  def main(args: Array[String]): Unit = {
    val json = Files.readAllBytes(Paths.get("config.json"))
    val parsed = parser.parse(new String(json, StandardCharsets.UTF_8)).right.get
    val index = new Index()
    val root = index.traverse(parsed)
    val roIndex = index.freeze()
    println("ROOT:"+ root)

    import ToBytes._
    import ToBytesVar._


    val collections = List(
    roIndex.ints.asSeq.bytes,
      roIndex.longs.asSeq.bytes,
      roIndex.floats.asSeq.bytes,
      roIndex.doubles.asSeq.bytes,
      roIndex.strings.asSeq.bytes,
      roIndex.arrs.asSeq.bytes,
      roIndex.objs.asSeq.bytes,
    )

    val headerLen = (2 + collections.length) * Integer.BYTES

    val offsets = collections.map(_.length).foldLeft(Vector(headerLen)) {
      case (offsets, currentSize) =>
        offsets :+ (offsets.head + currentSize)
    }
    assert(offsets.size == collections.size+1)
    val everything = Seq((Seq(0, collections.length) ++ offsets).bytes) ++ collections
    val blob = everything.foldLeft(ByteString.empty)(_ ++ _)

    val level = 20
    val raw = blob.toArray
    val compressed = Zstd.compress(raw, level)

    println(s"Raw size: ${raw.length / 1024} kB")
    println(s"Compressed size: ${compressed.length / 1024} kB (zstd level=$level)")

    Files.write(Paths.get("output.bin"), raw)
    Files.write(Paths.get("output.bin.zstd"), compressed)

  }
}
