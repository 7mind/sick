package io.izumi.sick

import com.github.luben.zstd.Zstd
import io.circe.*
import izumi.sick.SICK
import izumi.sick.indexes.SICKSettings
import izumi.sick.model.ToBytesFixed
import izumi.sick.sickcirce.CirceTraverser.*

import java.nio.charset.StandardCharsets
import java.nio.file.{Files, Paths}

object StrTool {
  implicit class StringExt(s: String) {
    def padLeft(p: Int, fill: Char): String = {
      s.reverse.padTo(p, fill).reverse
    }
  }
}

import io.izumi.sick.StrTool.*

case class Val(value: Int, pad: Int = 8) {
  override def toString: String = s"{0x${value.toHexString.padLeft(pad, ' ')}=${value.toString.padLeft(pad + 2, ' ')}}"
}





object Demo {
  val in = Paths.get("..", "samples")
  val out = Paths.get("..", "output")
  val rootname = "sample.json"
  out.toFile.mkdirs()

  def main(args: Array[String]): Unit = {
    import scala.jdk.CollectionConverters.*
    val allInputs = Files
      .walk(in)
      .map(_.toFile)
      .filter(_.isFile)
      .filter(_.getName.endsWith(".json"))
      .iterator()
      .asScala
      .toList

    allInputs.foreach {
      input =>
        println(s"Processing $input")
        val json = Files.readAllBytes(input.toPath)
        val parsed = parser.parse(new String(json, StandardCharsets.UTF_8)).toOption.get

        val eba = SICK.Default.pack(parsed, rootname)
        val roIndex = eba.index
        val root = eba.root
        val rwIndex = eba.source

        val newRoot = roIndex.findRoot(rootname).get.ref
        assert(roIndex.reconstruct(newRoot) == rwIndex.freeze(SICKSettings.default).reconstruct(root))

        println(s"Original root: $root -> $newRoot")

        println("Frozen:")
        roIndex.roots.data.foreach {
          case (k, v) =>
            println(s"ROOT ${roIndex.strings.data(k)}: $k->$v")
        }
        println(roIndex.summary)

        val packed = roIndex.packFile()

        val level = 20
        val raw = Files.readAllBytes(packed.data)
        assert(raw.length == packed.length)
        val compressed = Zstd.compress(raw, level)

        println("=" * 80)
        assert(roIndex.parts.size == packed.offsets.size)
        val szInt = implicitly[ToBytesFixed[Int]].blobSize

        println(
          s"Header: ${packed.headerLen} bytes, ${packed.headerLen / Integer.BYTES} integers, [version:int == ${packed.version}][collection_count:int == ${packed.offsets.size}][collection_offsets: int * ${packed.offsets.size}]"
        )
        println(s"Offsets (${packed.offsets.size}):")
//        roIndex.parts.zip(packed.offsets).foreach {
//          case (p, o) =>
//            val sz = p._1.data.size
//
//            val info = p._2 match {
//              case c: ToBytesFixed[_] =>
//                Some(s"[value:${c.blobSize} bytes]")
//              case c: ToBytesFixedArray[_] =>
//                Some(s"[count:int == ${Val(sz, 4)}][element: {${c.elementSize} bytes} * ${sz.toString.padLeft(7, '0')}]")
//              case _: ToBytesVar[_] =>
//                Some(s"[length:int][value:BYTESTR]")
//              case _: ToBytesVarArray[_] =>
//                val dataOffset = o + szInt + sz * szInt + szInt
//                Some(
//                  s"[count:int == ${Val(sz, 4)}][relative_element_offset: int * ${sz.toString.padLeft(7, ' ')}][relative_end_offset:int][element: BYTESTR * ${sz.toString
//                      .padLeft(7, ' ')}] data_offset = ${Val(dataOffset)}"
//                )
//            }
//
//            val tpe = p._2 match {
//              case fixed: ToBytesFixed[_] =>
//                s"Byte<${fixed.blobSize}>"
//              case fixed: ToBytesFixedArray[_] =>
//                s"Arr[Byte<${fixed.elementSize}>]"
//              case _: ToBytesVar[_] =>
//                "BYTESTR"
//              case _: ToBytesVarArray[_] =>
//                "Arr[BYTESTR]"
//            }
//            println(f"  ${p._1.name}%10s -> ${Val(o)}; $tpe%10s ${info.map(i => s"; $i").getOrElse("")}")
//        }
        println(s"Offsets: ${packed.offsets.mkString(", ")}")
        println("NOTE: relative element offsets should use corresponding data offsets as their base")
        println(
          "NOTE: data offsets should be computed the following way: (structure_start (from header)) + (int_size (of array_length)) + (int_size * array_length * int_size) + (int_size (of array_length))"
        )
        println(s"Raw size: ${raw.length} == ${raw.length / 1024} kB")
        println(s"Compressed size: ${compressed.length} == ${compressed.length / 1024} kB (zstd level=$level)")

        val fileName = input.getName
        val basename = if (fileName.indexOf(".") > 0) {
          fileName.substring(0, fileName.lastIndexOf("."))
        } else {
          fileName
        }
        Files.write(out.resolve(s"$basename-SCALA.bin"), raw)
      // Files.write(out.resolve(s"$basename-scala.bin.zstd"), compressed)

    }

  }

}
