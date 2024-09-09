package izumi.sick.indexes

import izumi.sick.model
import izumi.sick.model.*
import izumi.sick.tables.{DeduplicatingRefTableBuilder, GenericRefTableBuilder}

object IndexRW {
  def apply(dedup: Boolean): IndexRW = {
    val strings = GenericRefTableBuilder[String]("Strings", dedup = true)

    val ints = GenericRefTableBuilder[Int]("Integers", dedup = true)
    val longs = GenericRefTableBuilder[Long]("Longs", dedup = true)
    val bigints = GenericRefTableBuilder[BigInt]("Bigints", dedup = true)

    val floats = GenericRefTableBuilder[Float]("Floats", dedup = true)
    val doubles = GenericRefTableBuilder[Double]("Doubles", dedup = true)
    val bigDecimals = GenericRefTableBuilder[BigDecimal]("BigDecs", dedup = true)

    val arrs = GenericRefTableBuilder[Arr]("Arrays", dedup)
    val objs = GenericRefTableBuilder[Obj]("Objects", dedup)
    val roots = GenericRefTableBuilder[Root]("Roots", dedup)

    new IndexRW(
      strings,
      ints,
      longs,
      bigints,
      floats,
      doubles,
      bigDecimals,
      arrs,
      objs,
      roots,
    )
  }
}
class IndexRW private (
  strings: GenericRefTableBuilder[String],
  ints: GenericRefTableBuilder[Int],
  longs: GenericRefTableBuilder[Long],
  bigints: GenericRefTableBuilder[BigInt],
  floats: GenericRefTableBuilder[Float],
  doubles: GenericRefTableBuilder[Double],
  bigDecimals: GenericRefTableBuilder[BigDecimal],
  arrs: GenericRefTableBuilder[Arr],
  objs: GenericRefTableBuilder[Obj],
  roots: GenericRefTableBuilder[Root],
) {

  def freeze(settings: SICKSettings): IndexRO = {
    new IndexRO(
      settings,
      ints.freeze(),
      longs.freeze(),
      bigints.freeze(),
      floats.freeze(),
      doubles.freeze(),
      bigDecimals.freeze(),
      strings.freeze(),
      arrs.freeze(),
      objs.freeze(),
      roots.freeze(),
    )
  }

  override def toString: String = {
    Seq(strings, ints, longs, bigints, floats, doubles, bigDecimals, arrs, objs, roots).filterNot(_.isEmpty).mkString("\n\n")
  }

  /*def rebuild(): IndexRW = {
    def rebuildSimpleTable[V](table: GenericRefTableBuilder[V], tpe: RefKind) = {
      val data = table.enumerate().toSeq.zipWithIndex.map {
        case ((originalRef, (target)), newRef) =>
          (originalRef, (newRef, target))
      }
      val updated = GenericRefTableBuilder.fromMonotonic(table.name, data.map(_._2))
      (updated, data.map { case (origRef, (newRef, _)) => Ref(tpe, origRef) -> Ref(tpe, newRef) }.toMap)
    }

    val (newInts, intmapMap) = rebuildSimpleTable(ints, RefKind.TInt)
    val (newLongs, longMap) = rebuildSimpleTable(longs, RefKind.TLng)
    val (newBigints, bigintMap) = rebuildSimpleTable(bigints, RefKind.TBigInt)
    val (newFloats, floatMap) = rebuildSimpleTable(floats, RefKind.TFlt)
    val (newDoubles, doubleMap) = rebuildSimpleTable(doubles, RefKind.TDbl)
    val (newBigdecs, bigdecMap) = rebuildSimpleTable(bigDecimals, RefKind.TBigDec)
    val (newStrs, stringMap) = rebuildSimpleTable(strings, RefKind.TStr)
    val (newArrs, arrMap) = rebuildSimpleTable(arrs, RefKind.TArr)
    val (newObjs, objMap) = rebuildSimpleTable(objs, RefKind.TObj)
    val (newRoots, rootMap) = rebuildSimpleTable(roots, RefKind.TRoot)

    val fullMap = Seq(intmapMap, longMap, bigintMap, floatMap, doubleMap, bigdecMap, stringMap, arrMap, objMap, rootMap).flatten.toMap

    new IndexRW(
      newStrs,
      newInts,
      newLongs,
      newBigints,
      newFloats,
      newDoubles,
      newBigdecs,
      newArrs.remap(fullMap),
      newObjs.remap(fullMap),
      newRoots.remap(fullMap),
    )

  }*/

  def addString(s: String): Ref = model.Ref(RefKind.TStr, strings.insert(s))
  def addInt(s: Int): Ref = model.Ref(RefKind.TInt, ints.insert(s))
  def addLong(s: Long): Ref = model.Ref(RefKind.TLng, longs.insert(s))
  def addBigInt(s: BigInt): Ref = model.Ref(RefKind.TBigInt, bigints.insert(s))
  def addFloat(s: Float): Ref = model.Ref(RefKind.TFlt, floats.insert(s))
  def addDouble(s: Double): Ref = model.Ref(RefKind.TDbl, doubles.insert(s))
  def addBigDec(s: BigDecimal): Ref = model.Ref(RefKind.TBigDec, bigDecimals.insert(s))
  def addArr(s: Arr): Ref = model.Ref(RefKind.TArr, arrs.insert(s))
  def addObj(s: Obj): Ref = model.Ref(RefKind.TObj, objs.insert(s))
  def addRoot(s: Root): Ref = model.Ref(RefKind.TRoot, roots.insert(s))
}
