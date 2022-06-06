package izumi.sick


import akka.util.ByteString
import com.github.luben.zstd.Zstd
import io.circe._

import java.nio.charset.StandardCharsets
import java.nio.file.{Files, Paths}

sealed trait Foo
case class Bar(xs: Vector[String]) extends Foo
case class Qux(i: Long, d: Option[Double]) extends Foo


case class Val(value: Int, pad: Int = 8) {
  override def toString: String = s"{0x${value.toHexString.reverse.padTo(pad, '0').reverse}=${value.toString.reverse.padTo(pad+2, '0').reverse}}"
}

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
    assert(roIndex.parts.size == packed.offsets.size)
    println("Offsets:")
    roIndex.parts.zip(packed.offsets).foreach {
      case (p, o) =>
        val sz = p._1.data.size
        val szInt = implicitly[ToBytesFixed[Int]].blobSize

        val info = p._2 match {
          case c: ToBytesFixed[_] =>
            Some(s"[value:${c.blobSize} bytes]")
          case c: ToBytesFixedArray[_] =>
            Some(s"[count:int == $sz][element: $sz X ${c.elementSize} bytes]")
          case _: ToBytesVar[_] =>
            Some(s"[length:int][value:varbytes]")
          case _: ToBytesVarArray[_] =>
            val dataOffset = o + szInt + sz*szInt + szInt
            Some(s"data offset = ${Val(dataOffset)} [count:int == ${Val(sz, 4)}][relative_element_offset: ${sz.toString.reverse.padTo(7, '0').reverse} X $szInt bytes][count:int == ${Val(sz, 4)}][element: ${sz.toString.reverse.padTo(7, '0').reverse} X varbytes] ")
        }

        val tpe = p._2 match {
          case fixed: ToBytesFixed[_] =>
            s"Byte<${fixed.blobSize}>"
          case fixed: ToBytesFixedArray[_] =>
            s"Arr[Byte<${fixed.elementSize}>]"
          case _: ToBytesVar[_] =>
            "BYTESTR"
          case _: ToBytesVarArray[_] =>
            "Arr[BYTESTR]"
        }
        println(f"  ${p._1.name}%10s -> ${Val(o)}; $tpe%10s ${info.map(i => s"; $i").getOrElse("")}")
    }
    println(s"Offsets: ${packed.offsets.mkString(", ")}")
    println("NOTE: relative element offsets should use corresponding data offsets as their base")
    println("NOTE: data offsets should be computed the following way: (structure_start (from header)) + (int_size (of array_length)) + (int_size * array_length * int_size) + (int_size (of array_length))")
    println(s"Raw size: ${raw.length} == ${raw.length / 1024} kB")
    println(s"Compressed size: ${compressed.length} == ${compressed.length / 1024} kB (zstd level=$level)")

    Files.write(Paths.get("output.bin"), raw)
    Files.write(Paths.get("output.bin.zstd"), compressed)

  }

  private def packBlobs(collections: Seq[ByteString]): Packed = {
    import ToBytes._
    val headerLen = (2 + collections.length) * Integer.BYTES

    val offsets = computeOffsets(collections, headerLen)
    assert(offsets.size == collections.size)

    val everything = Seq((Seq(0, collections.length) ++ offsets).bytes.drop(Integer.BYTES)) ++ collections
    val blob = everything.foldLeft(ByteString.empty)(_ ++ _)
    Packed(offsets, blob)
  }


}

case class Packed(offsets: Seq[Int], data: ByteString)
