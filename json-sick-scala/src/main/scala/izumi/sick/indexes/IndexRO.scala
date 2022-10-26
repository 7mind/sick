package izumi.sick.indexes

import izumi.sick.indexes.IndexRO.Packed
import izumi.sick.model.{Arr, Obj, Root, ToBytes}
import izumi.sick.tables.RefTableRO
import izumi.sick.thirdparty.akka.util.ByteString

//trait AbstractIndex {
//  def getInt(index: RefVal): Int
//  def getLong(index: RefVal): Long
//  def getBigint(index: RefVal): BigInt
//  def getFloat(index: RefVal): Float
//  def getDouble(index: RefVal): Double
//  def getBigDecimal(index: RefVal): BigDecimal
//  def getString(index: RefVal): String
//  def getArr(index: RefVal): Arr
//  def getObj(index: RefVal): Obj
//  def getRoot(index: RefVal): Root
//}

final class IndexRO(
  val settings: PackSettings,
  val ints: RefTableRO[Int],
  val longs: RefTableRO[Long],
  val bigints: RefTableRO[BigInt],
  val floats: RefTableRO[Float],
  val doubles: RefTableRO[Double],
  val bigDecimals: RefTableRO[BigDecimal],
  val strings: RefTableRO[String],
  val arrs: RefTableRO[Arr],
  val objs: RefTableRO[Obj],
  val roots: RefTableRO[Root],
) {
  def findRoot(str: String): Option[Root] = {
    roots.asSeq.find(r => strings(r.id) == str)
  }

  def summary: String =
    s"""Index summary:
       |${parts.map(_._1).map(p => s"${p.name}: ${p.data.size}").mkString("\n")}""".stripMargin

  override def toString: String = {
    parts.map(_._1).filterNot(_.isEmpty).mkString("\n\n")
  }

  def pack(): Packed = pack(blobs, settings)

  private def blobs: Seq[ByteString] = parts.map {
    case (p, codec) =>
      codec.asInstanceOf[ToBytes[Any]].bytes(p.asSeq)
  }

  def parts: Seq[(RefTableRO[Any], ToBytes[Seq[Any]])] = {
    import izumi.sick.model.ToBytes._
    Seq(
      (ints, implicitly[ToBytes[Seq[Int]]]),
      (longs, implicitly[ToBytes[Seq[Long]]]),
      (bigints, implicitly[ToBytes[Seq[BigInt]]]),
      (floats, implicitly[ToBytes[Seq[Float]]]),
      (doubles, implicitly[ToBytes[Seq[Double]]]),
      (bigDecimals, implicitly[ToBytes[Seq[BigDecimal]]]),
      (strings, implicitly[ToBytes[Seq[String]]]),
      (arrs, implicitly[ToBytes[Seq[Arr]]]),
      (objs, toBytesFixedSizeArray(new ObjToBytes(strings, settings))),
      (roots, implicitly[ToBytes[Seq[Root]]]),
    ).map { case (c, codec) => (c.asInstanceOf[RefTableRO[Any]], codec.asInstanceOf[ToBytes[Seq[Any]]]) }
  }

  private def pack(collections: Seq[ByteString], settings: PackSettings): Packed = {
    import izumi.sick.model.ToBytes._
    val version = 0
    val headerLen = (2 + collections.length) * Integer.BYTES + java.lang.Short.BYTES

    val offsets = computeOffsets(collections, headerLen)
    assert(offsets.size == collections.size)

    val everything = Seq((Seq(version, collections.length) ++ offsets).bytes.drop(Integer.BYTES)) ++ Seq(settings.bucketCount.bytes) ++ collections
    val blob = everything.foldLeft(ByteString.empty)(_ ++ _)
    Packed(version, headerLen, offsets, blob)
  }
}

object IndexRO {
  final case class Packed(version: Int, headerLen: Int, offsets: Seq[Int], data: ByteString)
}

case class PackSettings(bucketCount: Short, limit: Short)

object PackSettings {
  def default = PackSettings(256, 2)
}
