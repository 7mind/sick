package izumi.sick


import akka.util.ByteString
import com.github.luben.zstd.Zstd
import io.circe._

import java.nio.charset.StandardCharsets
import java.nio.file.{Files, Paths}

sealed trait Foo
case class Bar(xs: Vector[String]) extends Foo
case class Qux(i: Long, d: Option[Double]) extends Foo


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
    val file = args.headOption.getOrElse("config.json")
    val json = Files.readAllBytes(Paths.get(file))
    val parsed = parser.parse(new String(json, StandardCharsets.UTF_8)).right.get
    val index = new Index()
    val root = index.traverse(parsed)
    val roIndex = index.freeze()
    println("ROOT:"+ root)
    println(roIndex.summary)



    val  packed = packBlobs(roIndex.blobs)

    val level = 20
    val raw = packed.data.toArray
    val compressed = Zstd.compress(raw, level)

    println("="*80)
    assert(roIndex.parts.size + 1 == packed.offsets.size)
    println("Offsets:")
    (roIndex.parts ++ List((new Reftable[Any]("END", Map.empty), null))).zip(packed.offsets).foreach {
      case (p, o) =>
        val sz = p._1.data.size
        val info = p._2 match {
          case c: ToBytesFixed[_] =>
            Some(s"[value:${c.blobSize} bytes]")
          case c: ToBytesFixedArray[_] =>
            Some(s"[count:int == $sz][element: $sz X ${c.elementSize} bytes]")
          case _: ToBytesVar[_] =>
            Some(s"[length:int][value:varbytes]")
          case _: ToBytesVarArray[_] =>
            Some(s"[count:int == $sz][element_length: $sz X ${implicitly[ToBytesFixed[Int]].blobSize} bytes][count:int == $sz][element: $sz X varbytes]")
          case _ => None
        }
        println(f"  ${p._1.name}%10s -> $o%8d ${info.map(i => s"( $i )").getOrElse("")}")
    }
    println(s"Offsets: ${packed.offsets.mkString(", ")}")
    println(s"Raw size: ${raw.length} == ${raw.length / 1024} kB")
    println(s"Compressed size: ${compressed.length} == ${compressed.length / 1024} kB (zstd level=$level)")

    Files.write(Paths.get("output.bin"), raw)
    Files.write(Paths.get("output.bin.zstd"), compressed)

  }

  private def packBlobs(collections: Seq[ByteString]): Packed = {
    import ToBytes._
    val headerLen = (2 + collections.length) * Integer.BYTES

    val offsets = collections.map(_.length).foldLeft(Vector(headerLen)) {
      case (offsets, currentSize) =>
        offsets :+ (offsets.last + currentSize)
    }
    assert(offsets.size == collections.size + 1)
    val everything = Seq((Seq(0, collections.length) ++ offsets).bytes) ++ collections
    val blob = everything.foldLeft(ByteString.empty)(_ ++ _)
    Packed(offsets, blob)
  }
}

case class Packed(offsets: Vector[Int], data: ByteString)
