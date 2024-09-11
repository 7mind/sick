package io.izumi.sick

import com.github.luben.zstd.Zstd
import io.circe.*
import izumi.sick.SICK
import izumi.sick.eba.SICKSettings
import izumi.sick.model.ArrayWriteStrategy.DoublePass
import izumi.sick.model.{ArrayWriteStrategy, SICKWriterParameters}
import izumi.sick.sickcirce.CirceTraverser.*
import izumi.sick.thirdparty.akka.util.ByteString

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
      .sortBy(f => f.length())

    def dprint(x: String) = { println(x) }

    Seq(ArrayWriteStrategy.DoublePass, ArrayWriteStrategy.SinglePassInMemory, ArrayWriteStrategy.StreamRepositioning).foreach {
      strategy =>
        var ccdedup: Int = 0
        var ccnodedup: Int = 0
        var sumDedup: Long = 0
        var sumNodedup: Long = 0

        allInputs.foreach {
          input =>
            dprint("=" * 80)
            dprint(s"Processing $input")
            val json = Files.readAllBytes(input.toPath)
            val parsed = parser.parse(new String(json, StandardCharsets.UTF_8)).toOption.get

            Seq(true, false).foreach {
              dedup =>
                dprint(s"dedup = $dedup")
                val before = System.nanoTime()
                val eba = SICK.Default.pack(parsed, rootname, dedup = dedup)
                val roIndex = eba.index
                val root = eba.root
                val rwIndex = eba.source
                val packed = roIndex.packFile(SICKWriterParameters(strategy))

                val raw = Files.readAllBytes(packed.data)

                strategy match {
                  case ArrayWriteStrategy.StreamRepositioning =>
                  case _ =>
                    val packedBytes = roIndex.packBytes(SICKWriterParameters(strategy))
                    assert(packedBytes.data == ByteString(raw))
                }
                val after = System.nanoTime()

                val newRoot = roIndex.findRoot(rootname).get.ref
                assert(roIndex.reconstruct(newRoot) == rwIndex.freeze(SICKSettings.default).reconstruct(root))

                dprint(s"Original root: $root -> $newRoot")
                dprint("Frozen:")
                roIndex.roots.data.foreach {
                  case (k, v) =>
                    dprint(s"ROOT ${roIndex.strings.data(k)}: $k->$v")
                }
                dprint(roIndex.summary)

                dprint(f"packing: dedup=$dedup, time = ${(after - before) / (1000 * 1000.0)}%2.2f msec")

                if (dedup) {
                  ccdedup += 1
                  sumDedup += after - before
                } else {
                  ccnodedup += 1
                  sumNodedup += after - before
                }
                val level = 20
                assert(raw.length == packed.length)

                val cbefore = System.nanoTime()
                val compressed = Zstd.compress(raw, level)
                val cafter = System.nanoTime()
                dprint(
                  f"compression (zstd=$level): dedup=$dedup, time = ${(cafter - cbefore) / (1000 * 1000.0)}%2.2f msec; ${raw.length}b => ${compressed.length}b (${raw.length / 1024.0}%2.2fkB => ${compressed.length / 1024.0}%2.2fkB)"
                )

                assert(roIndex.components.size == packed.offsets.size)

                val fileName = input.getName
                val basename = if (fileName.indexOf(".") > 0) {
                  fileName.substring(0, fileName.lastIndexOf("."))
                } else {
                  fileName
                }
                Files.write(out.resolve(s"$basename-SCALA-$strategy-$dedup.bin"), raw)
                // Files.write(out.resolve(s"$basename-scala.bin.zstd"), compressed)

                dprint("")
            }

        }

        println(s"strategy: $strategy")
        println(s"dedup average: ${sumDedup / ccdedup}")
        println(s"nodedup average: ${sumNodedup / ccnodedup}")
    }
  }

}
