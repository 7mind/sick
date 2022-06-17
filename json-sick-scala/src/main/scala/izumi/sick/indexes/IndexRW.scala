package izumi.sick.indexes

import izumi.sick.model
import izumi.sick.model._
import izumi.sick.tables.RefTableRW

object IndexRW {
  def apply(): IndexRW = {
    val strings = RefTableRW[String]("Strings")

    val ints = RefTableRW[Int]("Integers")
    val longs = RefTableRW[Long]("Longs")
    val bigints = RefTableRW[BigInt]("Bigints")

    val floats = RefTableRW[Float]("Floats")
    val doubles = RefTableRW[Double]("Doubles")
    val bigDecimals = RefTableRW[BigDecimal]("BigDecs")

    val arrs = RefTableRW[Arr]("Arrays")
    val objs = RefTableRW[Obj]("Objects")
    val roots = RefTableRW[Root]("Roots")
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
  strings: RefTableRW[String],
  ints: RefTableRW[Int],
  longs: RefTableRW[Long],
  bigints: RefTableRW[BigInt],
  floats: RefTableRW[Float],
  doubles: RefTableRW[Double],
  bigDecimals: RefTableRW[BigDecimal],
  arrs: RefTableRW[Arr],
  objs: RefTableRW[Obj],
  roots: RefTableRW[Root],
) {

  def freeze(): IndexRO = {
    new IndexRO(
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

  def rebuild(): IndexRW = {
    def rebuildSimpleTable[V](table: RefTableRW[V], tpe: RefKind) = {
      val data = table.all().toSeq.sortBy(_._2._2)(Ordering.Int.reverse).zipWithIndex.map {
        case ((originalRef, (target, freq)), newRef) =>
          (originalRef, (newRef, target, freq))
      }
      val updated = RefTableRW.fromMonotonic(table.name, data.map(_._2))
      (updated, data.map { case (origRef, (newRef, _, _)) => Ref(tpe, origRef) -> Ref(tpe, newRef) }.toMap)
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

  }

  def addString(s: String): Ref = model.Ref(RefKind.TStr, strings.add(s))
  def addInt(s: Int): Ref = model.Ref(RefKind.TInt, ints.add(s))
  def addLong(s: Long): Ref = model.Ref(RefKind.TLng, longs.add(s))
  def addBigInt(s: BigInt): Ref = model.Ref(RefKind.TBigInt, bigints.add(s))
  def addFloat(s: Float): Ref = model.Ref(RefKind.TFlt, floats.add(s))
  def addDouble(s: Double): Ref = model.Ref(RefKind.TDbl, doubles.add(s))
  def addBigDec(s: BigDecimal): Ref = model.Ref(RefKind.TBigDec, bigDecimals.add(s))
  def addArr(s: Arr): Ref = model.Ref(RefKind.TArr, arrs.add(s))
  def addObj(s: Obj): Ref = model.Ref(RefKind.TObj, objs.add(s))
  def addRoot(s: Root): Ref = {
    roots.revGet(s) match {
      case Some(value) =>
        throw new IllegalStateException(s"Root $s already exists with ref $value")
      case None =>
        model.Ref(RefKind.TRoot, roots.add(s))
    }
  }

}
