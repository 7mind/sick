package izumi.sick

import com.github.luben.zstd.Zstd
import io.circe._
import izumi.sick.indexes.IndexRW
import izumi.sick.model.{ToBytesFixed, ToBytesFixedArray, ToBytesVar, ToBytesVarArray}
import izumi.sick.sickcirce.CirceTraverser._

import java.nio.charset.StandardCharsets
import java.nio.file.{Files, Paths}
import scala.util.Random

sealed trait Foo
case class Bar(xs: Vector[String]) extends Foo
case class Qux(i: Long, d: Option[Double]) extends Foo

object StrTool {
  implicit class StringExt(s: String) {
    def padLeft(p: Int, fill: Char): String = {
      s.reverse.padTo(p, fill).reverse
    }
  }
}

import izumi.sick.StrTool._

case class Val(value: Int, pad: Int = 8) {
  override def toString: String = s"{0x${value.toHexString.padLeft(pad, ' ')}=${value.toString.padLeft(pad + 2, ' ')}}"
}

object Gen {

  def main(args: Array[String]): Unit = {
    val sb = new StringBuilder()
    sb.append("using Zenject;\n")
    val max = 1000
    val cargs = 15;
    val fwdlinks = 1;
    (1 to max).foreach {
      idx =>
        val prevs = (0 to cargs)
          .map(_ => Random.nextInt(idx))
          .toSet
          .filter(i => i < idx && i > 0)
          .map { i: Int => s"IFoo$i arg$i" }.mkString(", ")

        val fwds = (0 to fwdlinks)
          .map(_ => Random.nextInt(max))
          .toSet
          .filter(i => i > idx)
          .map {
            i: Int =>
              s"""
                 |    [Inject]
                 |    public IFoo$i Foo${i}Prop
                 |    {
                 |        get;
                 |        private set;
                 |    }
                 |""".stripMargin
          }.mkString("\n")

        sb.append(s"""
                     |public interface IFoo$idx {}
                     |
                     |public class Foo$idx : IFoo$idx
                     |{
                     |
                     |    $fwds
                     |    
                     |    public Foo$idx($prevs)
                     |    {
                     |    }
                     |}
                     |""".stripMargin)
    }

//    println(sb.toString())

    Files.write(Paths.get("/home/pavel/work/random/zenject-test/zenject-test/zenject-test/Boilerplate.cs"), sb.toString().getBytes())

    val sb1 = new StringBuilder()
    sb1.append(s"""
                  |using Zenject;
                  |
                  |public class Entrypoint0
                  |{
                  |    public static void Run(string[] args)
                  |    {
                  |     DiContainer container = new DiContainer();
                  |     
                  |""".stripMargin)
    (1 to max).foreach {
      idx =>
        sb1.append(s"        container.Bind<IFoo$idx>().To<Foo$idx>().AsSingle().NonLazy();\n")
    }

    (1 to 10)
      .map(_ => Random.nextInt(max - 1) + 1)
      .toSet
      .map { i: Int => s"if (container.Resolve<IFoo$i>() == null) { throw new Exception(); }" }
      .foreach {
        s => (0 to 1).foreach { a => sb1.append(s); sb1.append("\n") }
      }

    sb1.append("        }\n    }")

    Files.write(Paths.get("/home/pavel/work/random/zenject-test/zenject-test/zenject-test/Entrypoint.cs"), sb1.toString().getBytes())

  }
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
    val rwIndex = IndexRW()
    val root = rwIndex.append("config.json", parsed)

    val roIndex = rwIndex.rebuild().freeze()

    val newRoot = roIndex.findRoot("config.json").get.ref
    assert(roIndex.reconstruct(newRoot) == rwIndex.freeze().reconstruct(root))

    println(s"Original root: $root -> $newRoot")

    println("Frozen:")
    roIndex.roots.data.foreach {
      case (k, v) =>
        println(s"ROOT ${roIndex.strings.data(k)}: $k->$v")
    }
    println(roIndex.summary)

    val packed = roIndex.pack()

    val level = 20
    val raw = packed.data.toArray
    val compressed = Zstd.compress(raw, level)

//    println(roIndex.arrs.data(0))
//    println(roIndex.objs.data(0))
    println("=" * 80)
    assert(roIndex.parts.size == packed.offsets.size)
    val szInt = implicitly[ToBytesFixed[Int]].blobSize

    println(
      s"Header: ${packed.headerLen} bytes, ${packed.headerLen / Integer.BYTES} integers, [version:int == ${packed.version}][collection_count:int == ${packed.offsets.size}][collection_offsets: int * ${packed.offsets.size}]"
    )
    println(s"Offsets (${packed.offsets.size}):")
    roIndex.parts.zip(packed.offsets).foreach {
      case (p, o) =>
        val sz = p._1.data.size

        val info = p._2 match {
          case c: ToBytesFixed[_] =>
            Some(s"[value:${c.blobSize} bytes]")
          case c: ToBytesFixedArray[_] =>
            Some(s"[count:int == ${Val(sz, 4)}][element: {${c.elementSize} bytes} * ${sz.toString.padLeft(7, '0')}]")
          case _: ToBytesVar[_] =>
            Some(s"[length:int][value:BYTESTR]")
          case _: ToBytesVarArray[_] =>
            val dataOffset = o + szInt + sz * szInt + szInt
            Some(s"[count:int == ${Val(sz, 4)}][relative_element_offset: int * ${sz.toString.padLeft(7, ' ')}][relative_end_offset:int][element: BYTESTR * ${sz.toString
                .padLeft(7, ' ')}] data_offset = ${Val(dataOffset)}")
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
    println(
      "NOTE: data offsets should be computed the following way: (structure_start (from header)) + (int_size (of array_length)) + (int_size * array_length * int_size) + (int_size (of array_length))"
    )
    println(s"Raw size: ${raw.length} == ${raw.length / 1024} kB")
    println(s"Compressed size: ${compressed.length} == ${compressed.length / 1024} kB (zstd level=$level)")

    Files.write(Paths.get("output.bin"), raw)
    Files.write(Paths.get("output.bin.zstd"), compressed)

  }

}
