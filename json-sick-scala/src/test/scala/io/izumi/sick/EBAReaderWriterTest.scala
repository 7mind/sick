package io.izumi.sick

import com.github.luben.zstd.Zstd
import io.circe.*
import izumi.sick.SICK
import izumi.sick.eba.reader.EBAReader
import izumi.sick.eba.writer.{EBAEncoders, EBAWriter}
import izumi.sick.eba.{EBAStructure, SICKSettings}
import izumi.sick.model.{RefKind, SICKWriterParameters, TableWriteStrategy}
import izumi.sick.sickcirce.CirceTraverser.*
import izumi.sick.thirdparty.akka.util.ByteString
import org.scalatest.wordspec.AnyWordSpec

import java.nio.charset.StandardCharsets
import java.nio.file.{Files, Path, Paths}
import scala.jdk.CollectionConverters.*

class EBAReaderWriterTest extends AnyWordSpec {
  private val in: Path = Paths.get("..", "samples")
  private val out: Path = Paths.get("..", "output")
  private val rootname = "sample.json"

  "write test" in {
    val allInputs = Files
      .walk(in)
      .map(_.toFile)
      .filter(_.isFile)
      .filter(_.getName.endsWith(".json"))
      .iterator()
      .asScala
      .toList
      .sortBy(f => f.length())

    out.toFile.mkdirs()

    Seq(TableWriteStrategy.DoublePass, TableWriteStrategy.SinglePassInMemory, TableWriteStrategy.StreamRepositioning).foreach {
      strategy =>
        var ccdedup: Int = 0
        var ccnodedup: Int = 0
        var sumDedup: Long = 0
        var sumNodedup: Long = 0

        allInputs.foreach {
          input =>
            println("=" * 80)
            println(s"Processing $input")
            val json = Files.readAllBytes(input.toPath)
            val parsed = parser.parse(new String(json, StandardCharsets.UTF_8)).toOption.get

            Seq(true, false).foreach {
              dedup =>
                println(s"dedup = $dedup")
                val before = System.nanoTime()
                val eba = SICK.Default.packJson(parsed, rootname, dedup = dedup)
                val roIndex = eba.index
                val root = eba.root
                val rwIndex = eba.source
                val (dataFile, packedFileInfo) = EBAWriter.writeTempFile(roIndex, SICKWriterParameters(strategy))

                val raw =
                  try Files.readAllBytes(dataFile)
                  finally Files.delete(dataFile)

                strategy match {
                  case TableWriteStrategy.StreamRepositioning =>
                  case _ =>
                    val (packedBytes, _) = EBAWriter.writeBytes(roIndex, SICKWriterParameters(strategy))
                    assert(packedBytes == ByteString(raw))
                }
                val after = System.nanoTime()

                val newRoot = roIndex.findRoot(rootname).get.ref
                assert(roIndex.reconstruct(newRoot) == rwIndex.freeze(SICKSettings.default).reconstruct(root))

                println(s"Original root: $root -> $newRoot")
                println("Frozen:")
                roIndex.roots.data.foreach {
                  case (k, v) =>
                    println(s"ROOT ${roIndex.strings.data(k)}: $k->$v")
                }
                println(roIndex.summary)

                println(f"packing: dedup=$dedup, time = ${(after - before) / (1000 * 1000.0)}%2.2f msec")

                if (dedup) {
                  ccdedup += 1
                  sumDedup += after - before
                } else {
                  ccnodedup += 1
                  sumNodedup += after - before
                }
                val level = 20
                assert(raw.length == packedFileInfo.length)

                val cbefore = System.nanoTime()
                val compressed = Zstd.compress(raw, level)
                val cafter = System.nanoTime()
                println(
                  f"compression (zstd=$level): dedup=$dedup, time = ${(cafter - cbefore) / (1000 * 1000.0)}%2.2f msec; ${raw.length}b => ${compressed.length}b (${raw.length / 1024.0}%2.2fkB => ${compressed.length / 1024.0}%2.2fkB)"
                )

                assert(roIndex.tables.size == packedFileInfo.offsets.size)

                val fileName = input.getName
                val basename = if (fileName.indexOf(".") > 0) {
                  fileName.substring(0, fileName.lastIndexOf("."))
                } else {
                  fileName
                }
                val outFile = out.resolve(s"$basename-SCALA-$strategy-$dedup.bin")
                Files.write(outFile, raw)
                // Files.write(out.resolve(s"$basename-scala.bin.zstd"), compressed)

                val readStructure: EBAStructure = EBAReader.readEBAStructure(Files.readAllBytes(outFile), new EBAEncoders(SICKWriterParameters(strategy)))
                assert(readStructure == roIndex)

                println()
            }
        }

        println(s"strategy: $strategy")
        println(s"dedup average: ${sumDedup / ccdedup}")
        println(s"nodedup average: ${sumNodedup / ccnodedup}")
    }
  }

  "read test" in {
    val inputs: Seq[Path] = {
      val ds = Files.newDirectoryStream(out, "*.bin")
      try ds.asScala.toSeq
      finally ds.close()
    }.sortBy(_.toString)

    //    assert(inputs.exists(_.toString.contains("-CS")), "No file containing '-CS' found!")
    assert(inputs.exists(_.toString.contains("-SCALA")), "No file containing '-SCALA' found!")

    for (fpath <- inputs) {
      val fname = fpath.getFileName.toString
      println(s"Processing $fname (${fpath.toFile.length()} bytes) ...")

      try {
        val eba: EBAStructure = EBAReader.readEBAStructure(Files.readAllBytes(fpath), new EBAEncoders(SICKWriterParameters()))

        val maybeRoot = eba.findRoot(rootname)
        assert(maybeRoot.isDefined, s"No root entry in $fname")
        val rootRef = maybeRoot.get
        println(s"$fname: found $rootname, ref=$rootRef")

        rootRef.ref.kind match {
          case RefKind.TObj =>
            val objValue = eba.objs(rootRef.ref.ref)
            println(s"$fname: object with ${objValue.values.size} elements")
            println(objValue.values.mkString("  ", "\n  ", ""))
          case RefKind.TArr =>
            val arrValue = eba.arrs(rootRef.ref.ref)
            println(s"$fname: array with ${arrValue.values.size} elements")
            println(arrValue.values.mkString("  ", "\n  ", ""))
          case x =>
            throw new RuntimeException(x.toString)
        }
        println()

      } catch {
        case ex: Throwable =>
          println(s"Failed on $fpath")
          println()
          throw ex
      }
    }
  }

}
