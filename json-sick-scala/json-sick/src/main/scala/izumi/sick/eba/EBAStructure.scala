package izumi.sick.eba

import izumi.sick.model.*

import scala.collection.immutable.ArraySeq

/** RO index */
final case class EBAStructure(
  ints: EBATable[Int],
  longs: EBATable[Long],
  bigints: EBATable[BigInt],
  floats: EBATable[Float],
  doubles: EBATable[Double],
  bigDecimals: EBATable[BigDecimal],
  strings: EBATable[String],
  arrs: EBATable[Arr],
  objs: EBATable[Obj],
  roots: EBATable[Root],
)(val settings: SICKSettings
) {
  val tables: ArraySeq[EBATable[Any]] = ArraySeq(
    ints,
    longs,
    bigints,
    floats,
    doubles,
    bigDecimals,
    strings,
    arrs,
    objs,
    roots,
  )

  def findRoot(str: String): Option[Root] = {
    roots.asIterable.find(r => strings(r.id) == str)
  }

  def summary: String = {
    s"""Index summary:
       |  ${tables.map(p => s"${p.name}: ${p.data.size}").mkString("\n")}""".stripMargin
  }

  override def toString: String = {
    tables.filterNot(_.isEmpty).mkString("\n\n")
  }
}
