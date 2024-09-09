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
  def rewrite(mapping: V => V): GenericRefTableBuilder[V]
}

object GenericRefTableBuilder {
  def apply[V](name: String, dedup: Boolean): GenericRefTableBuilder[V] = {
    if (dedup) {
      val reverse = mutable.HashMap.empty[V, RefVal]
      new DeduplicatingRefTableBuilder[V](name, reverse)
    } else {
      val content = mutable.HashMap.empty[RefVal, V]
      new QuickRefTableBuilder[V](name, content)
    }
  }
}
