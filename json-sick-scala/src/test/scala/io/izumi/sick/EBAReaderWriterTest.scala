package io.izumi.sick

import com.github.luben.zstd.Zstd
import io.circe.*
import izumi.sick.SICK
import izumi.sick.eba.reader.incremental.IncrementalJValue
import izumi.sick.eba.reader.{EagerEBAReader, IncrementalEBAReader}
import izumi.sick.eba.writer.EBAWriter
import izumi.sick.eba.writer.codecs.EBACodecs.DebugTableName
import izumi.sick.eba.{EBAStructure, SICKSettings}
import izumi.sick.model.*
import izumi.sick.sickcirce.CirceTraverser.*
import izumi.sick.thirdparty.akka.util.ByteString
import org.scalatest.wordspec.AnyWordSpec

import java.nio.charset.StandardCharsets
import java.nio.file.{Files, Path, Paths}
import java.util.concurrent.atomic.AtomicInteger
import scala.annotation.tailrec
import scala.concurrent.duration.{FiniteDuration, NANOSECONDS}
import scala.jdk.CollectionConverters.*

class EBAReaderWriterTest extends AnyWordSpec {
  private val in: Path = Paths.get("..", "samples")
  private val out: Path = Paths.get("..", "output")
  private val rootname = "sample.json"
  private val iters = 100_000

  // NB: The tests are executed sequentially and due to temporal interdependency should be left sequential (unless fixed)

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

        val tableCounters = DebugTableName.allTableNames.map(_ -> new AtomicInteger(0)).toMap

        allInputs.foreach {
          input =>
            println("=" * 80)
            println(s"Processing $input")
            val json = Files.readAllBytes(input.toPath)
            val parsed = parser.parse(new String(json, StandardCharsets.UTF_8)).toOption.get

            Seq(true, false).flatMap(x => Seq((x, true), (x, false))).flatMap(y => Seq((y._1, y._2, true), (y._1, y._2, false))).foreach {
              case (dedup, dedupPrimitives, avoidBigDecimals) =>
                println(s"dedup = $dedup dedupPrims = $dedupPrimitives noBigDec = $avoidBigDecimals")
                val before = System.nanoTime()
                val eba = SICK.packJson(parsed, rootname, dedup = dedup, dedupPrimitives = dedupPrimitives, avoidBigDecimals = avoidBigDecimals)
                val roIndex = eba.index
                val root = eba.root
                val rwIndex = eba.source
                val (dataFile, packedFileInfo) = EBAWriter.writeTempFile(roIndex, SICKWriterParameters(strategy))

                val raw =
                  try Files.readAllBytes(dataFile)
                  finally Files.delete(dataFile)

                val bytesStrategy = strategy match {
                  case TableWriteStrategy.StreamRepositioning => TableWriteStrategy.SinglePassInMemory
                  case _ => strategy
                }
                val (packedBytes, _) = EBAWriter.writeBytes(roIndex, SICKWriterParameters(bytesStrategy))
                Predef.assert(
                  packedBytes == ByteString(raw),
                  packedBytes.iterator.zipWithIndex.find {
                    case (b, i) => i > raw.length || raw(i) != b
                  },
                )
                val after = System.nanoTime()

                val newRoot = roIndex.findRoot(rootname).get.ref
                assert(roIndex.reconstruct(newRoot) == rwIndex.freeze(SICKSettings.default).reconstruct(root))

                println(s"Original root: $root -> $newRoot")
                println("Frozen:")
                roIndex.roots.forEach {
                  case (v, k) =>
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

                println(s"Eager reading...")
                val readStructure: EBAStructure = EagerEBAReader.readEBAStructure(Files.newInputStream(outFile))
                val eagerJson = readStructure.reconstruct(readStructure.roots.data.head.ref)
                assert(readStructure == roIndex)
                assertSameJson(avoidBigDecimals, parsed, eagerJson)
                println(s"Eager reading succeeded")

                println(s"Incremental reading...")
                val (incStructure: EBAStructure, incJson: Json) = {
                  val reader = IncrementalEBAReader.openFile(outFile, eagerOffsets = true)
                  try (reader.readAll(), reader.resolveFull(reader.roots.head._2))
                  finally reader.close()
                }
                assert(incStructure == roIndex)
                assert(incStructure == readStructure)
                assertSameJson(avoidBigDecimals, incJson, parsed)
                println(s"Incremental reading succeeded")

                incStructure.tables.foreach {
                  table => tableCounters(table.name).addAndGet(table.data.size)
                }

                println()
            }
        }

        tableCounters.foreach {
          case (k, vSize) =>
            assert(vSize.get() > 0, s"Table `$k` is not tested!")
        }

        println(s"strategy: $strategy")
        println(s"dedup average: ${sumDedup / ccdedup}")
        println(s"nodedup average: ${sumNodedup / ccnodedup}")
    }
  }

  "eager read test #2" in {
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
        val eba: EBAStructure = EagerEBAReader.readEBAStructure(Files.newInputStream(fpath))

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

  "incremental read test #2" in {
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

      val reader = IncrementalEBAReader.openFile(fpath, eagerOffsets = false)
      try {
        val rootRef = reader.getRoot(rootname).get
        println(s"$fname: found $rootname, ref=$rootRef")

        println(s"Going to perform $iters traverses")
        val begin = System.nanoTime()
        var i = 0
        while (i < iters) {
          traverse(rootRef, reader, 0, 10)
          i += 1
        }
        val end = System.nanoTime()
        val seconds = FiniteDuration(end - begin, NANOSECONDS).toNanos / 1000L / 1000.0 / 1000.0
        println(s"Finished in $seconds sec")
        println(s"Iters/sec ${iters.toDouble / seconds}")
        println(s"$fname: found $rootname, ref=$rootRef")

        rootRef.kind match {
          case RefKind.TObj =>
            println(s"$fname: object with ${reader.resolve(rootRef).asInstanceOf[IncrementalJValue.JObj].obj.length} elements")
          case _ =>
        }
        println()

        val eba = EagerEBAReader.readEBAFile(fpath)
        assert(reader.readAll() == eba)

        println("Succeeded comparison with eager reader")
      } catch {
        case ex: Throwable =>
          println(s"Failed on $fpath")
          println()
          throw ex
      } finally {
        reader.close()
      }
    }
  }

  @tailrec private def traverse(ref: Ref, reader: IncrementalEBAReader, count: Int, limit: Int): Int = {
    val readFirst = false
    val next = count + 1
    if (count >= limit) {
      count
    } else if (ref.kind == RefKind.TArr) {
      val arr = reader.resolve(ref).asInstanceOf[IncrementalJValue.JArr].arr
      if (arr.length == 0) {
        return next
      }

      val (entry: Ref, index: Int) = {
        if (readFirst) {
          (arr.iterator.next(), 0)
        } else {
          val index = arr.length / 2
          (arr.iterator.drop(index).next(), index)
        }
      }

      val entryRef = reader.readArrayElementRef(ref, index)
      assert(entry == entryRef)
      traverse(entryRef, reader, next, limit)
    } else if (ref.kind == RefKind.TObj) {
      val obj = reader.resolve(ref).asInstanceOf[IncrementalJValue.JObj].obj
      if (obj.length == 0) {
        return next
      }

      val (key: String, value: Ref) = {
        if (readFirst) {
          obj.iterator.next()
        } else {
          val index = obj.length / 2
          obj.iterator.drop(index).next()
        }
      }
      val fieldVal = reader.readObjectFieldRef(ref, key)
      assert(fieldVal == value)
      traverse(value, reader, next, limit)
    } else count
  }

  private def assertSameJson(avoidBigDecimals: Boolean, parsedJson: Json, sickJson: Json): Unit = {
    assert(sickJson.hcursor.keys.toSet.flatten == parsedJson.hcursor.keys.toSet.flatten)
    sickJson.hcursor.keys.iterator.flatten.foreach {
      k =>
        val egVal = sickJson.hcursor.downField(k).focus.get
        val parsedVal = parsedJson.hcursor.downField(k).focus.get
        if (avoidBigDecimals && (egVal != parsedVal)) {
          assert(egVal.isNumber && parsedVal.isNumber)
          assert(egVal.asNumber.get.toDouble == parsedVal.asNumber.get.toDouble)
        } else {
          assert(egVal == parsedVal, s"${egVal.asNumber.fold("": Any)(_.getClass)} ${parsedVal.asNumber.fold("": Any)(_.getClass)}")
        }
    }
  }

}
