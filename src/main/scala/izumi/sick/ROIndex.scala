package izumi.sick

import akka.util.ByteString

final class ROIndex(

               val bytes: Reftable[Byte],
               val shorts: Reftable[Short],
               val ints: Reftable[Int],
               val longs: Reftable[Long],

               val bigints: Reftable[BigInt],
               val floats: Reftable[Float],
               val doubles: Reftable[Double],
               val bigDecimals: Reftable[BigDecimal],

               val strings: Reftable[String],
               val arrs: Reftable[Arr],
               val objs: Reftable[Obj],
               val roots: Reftable[Root],
             ) {
  def parts: Seq[(Reftable[Any], ToBytes[Seq[Any]])] = {
    import ToBytes._
    Seq((bytes, implicitly[ToBytes[Seq[Byte]]]),
      (shorts, implicitly[ToBytes[Seq[Short]]]),
      (ints, implicitly[ToBytes[Seq[Int]]]),
      (longs, implicitly[ToBytes[Seq[Long]]]),
      (bigints, implicitly[ToBytes[Seq[BigInt]]]),
      (floats, implicitly[ToBytes[Seq[Float]]]),
      (doubles, implicitly[ToBytes[Seq[Double]]]),
      (bigDecimals, implicitly[ToBytes[Seq[BigDecimal]]]),
      (strings, implicitly[ToBytes[Seq[String]]]),
      (arrs, implicitly[ToBytes[Seq[Arr]]]),
      (objs, implicitly[ToBytes[Seq[Obj]]]),
      (roots, implicitly[ToBytes[Seq[Root]]]),
    ).map {case (c, codec) => (c.asInstanceOf[Reftable[Any]], codec.asInstanceOf[ToBytes[Seq[Any]]])}
  }

  def  blobs: Seq[ByteString] = parts.map {
    case (p, codec) =>
      codec.asInstanceOf[ToBytes[Any]].bytes(p.asSeq)
  }

  def summary: String =
    s"""Index summary:
       |${parts.map(_._1).map(p => s"${p.name}: ${p.data.size}").mkString("\n")}""".stripMargin

  override def toString: String = {
    parts.map(_._1).filterNot(_.isEmpty).mkString("\n\n")
  }

}
