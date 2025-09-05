package izumi.sick.eba.builder

import izumi.sick.eba.{EBAStructure, SICKSettings}
import izumi.sick.model.*

/** RW index */
class EBABuilder private (
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

  def freeze(settings: SICKSettings): EBAStructure = {
    new EBAStructure(
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
    )(settings)
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

  def addString(s: String): Ref = Ref(RefKind.TStr, strings.insert(s))
  def addInt(s: Int): Ref = Ref(RefKind.TInt, ints.insert(s))
  def addLong(s: Long): Ref = Ref(RefKind.TLng, longs.insert(s))
  def addBigInt(s: BigInt): Ref = Ref(RefKind.TBigInt, bigints.insert(s))
  def addFloat(s: Float): Ref = Ref(RefKind.TFlt, floats.insert(s))
  def addDouble(s: Double): Ref = Ref(RefKind.TDbl, doubles.insert(s))
  def addBigDec(s: BigDecimal): Ref = Ref(RefKind.TBigDec, bigDecimals.insert(s))
  def addArr(s: Arr): Ref = Ref(RefKind.TArr, arrs.insert(s))
  def addObj(s: Obj): Ref = Ref(RefKind.TObj, objs.insert(s))
  def addRoot(s: Root): Ref = Ref(RefKind.TRoot, roots.insert(s))
}

object EBABuilder {
  def apply(dedup: Boolean, dedupPrimitives: Boolean): EBABuilder = {
    val strings = GenericRefTableBuilder[String](dedup = dedupPrimitives)

    val ints = GenericRefTableBuilder[Int](dedup = dedupPrimitives)
    val longs = GenericRefTableBuilder[Long](dedup = dedupPrimitives)
    val bigints = GenericRefTableBuilder[BigInt](dedup = dedupPrimitives)

    val floats = GenericRefTableBuilder[Float](dedup = dedupPrimitives)
    val doubles = GenericRefTableBuilder[Double](dedup = dedupPrimitives)
    val bigDecimals = GenericRefTableBuilder[BigDecimal](dedup = dedupPrimitives)

    val arrs = GenericRefTableBuilder[Arr](dedup)
    val objs = GenericRefTableBuilder[Obj](dedup)
    val roots = GenericRefTableBuilder[Root](dedup)

    new EBABuilder(
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
