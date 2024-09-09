package izumi.sick.tables

import izumi.sick.model.Ref.RefVal

import scala.collection.mutable

trait GenericRefTableBuilder[V] {
  def name: String
  def insert(v: V): RefVal
  def enumerate(): Map[RefVal, V]
  def isEmpty: Boolean
  def size: Int
  def freeze(): RefTableRO[V]
  def rewrite(mapping: V => V): RefTableRW[V]
}

class RefTableRW[V] private (
  val name: String,
  reverse: mutable.HashMap[V, RefVal],
) extends GenericRefTableBuilder[V] {

  def insert(v: V): RefVal = {
    reverse.get(v) match {
      case Some(value) =>
        value
      case None =>
        val k = reverse.size: RefVal
        reverse.put(v, k)
        k
    }
  }

  def enumerate(): Map[RefVal, V] = {
    reverse.map {
      case (v, k) =>
        (k, v)
    }.toMap
  }

  def isEmpty: Boolean = reverse.isEmpty

  def size: Int = reverse.size

  def freeze(): RefTableRO[V] = new RefTableRO[V](name, reverse.map(_.swap).toMap)

//  def revGet(k: V): Option[RefVal] = reverse.get(k)

  def rewrite(mapping: V => V): RefTableRW[V] = {
    new RefTableRW[V](
      name,
      reverse.view.map { case (k, v) => mapping(k) -> v }.to(mutable.HashMap),
    )
  }

  override def toString: String = {
    s"""$name:
       |${reverse.map { case (v, k) => s"$k --> $v" }.mkString("\n")}""".stripMargin
  }
}

object RefTableRW {
  def apply[V](name: String): RefTableRW[V] = {
    val reverse = mutable.HashMap.empty[V, RefVal]
    new RefTableRW[V](name, reverse)
  }

  /*trait RefMappable[V] {
    def remap(value: V, mapping: Map[Ref, Ref]): V
  }
  object RefMappable {
    implicit object ArrMap extends RefMappable[Arr] {
    override def remap(value: Arr, mapping: Map[Ref, Ref]): Arr = {
      Arr(value.values.map(RefMappable.refRemap(_)(mapping.apply)))
    }
  }
  implicit object ObjMap extends RefMappable[Obj] {
    override def remap(value: Obj, mapping: Map[Ref, Ref]): Obj = {
      Obj(value.values.map { case (k, v) => (mapping(Ref(RefKind.TStr, k)).ref, RefMappable.refRemap(v)(mapping.apply)) })
    }
  }

  implicit object RootMap extends RefMappable[Root] {
    override def remap(value: Root, mapping: Map[Ref, Ref]): Root = {
      Root(mapping(Ref(RefKind.TStr, value.id)).ref, RefMappable.refRemap(value.ref)(mapping.apply))
    }
  }

    def refRemap(ref: Ref)(remap: Ref => Ref): Ref = {
      ref.kind match {
        case _: RefKind.NonMappable => ref
        case _: RefKind.Mappable => remap(ref)
      }
    }
  }

  implicit class Remap[V: RefMappable](bijection: RefTableRW[V]) {
    def remap(fullMap: Map[Ref, Ref]): RefTableRW[V] = {
      bijection.rewrite(v => implicitly[RefMappable[V]].remap(v, fullMap))
    }
  }

  def fromMonotonic[V](name: String, content: Seq[(Int, V)]): RefTableRW[V] = {
    assert(content.map(_._1) == content.indices)
    val out = new RefTableRW[V](
      name,
      content.map(_.swap).to(mutable.HashMap),
    )
    out
  }*/
}
