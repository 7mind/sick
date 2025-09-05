package izumi.sick.eba.builder

import izumi.sick.eba.EBATable
import izumi.sick.eba.writer.codecs.EBACodecs.DebugTableName
import izumi.sick.model.Ref.RefVal

import scala.collection.mutable
import scala.reflect.ClassTag

trait GenericRefTableBuilder[V] {
  def name: String
  def insert(v: V): RefVal
  def enumerate(): Map[RefVal, V]
  def isEmpty: Boolean
  def size: Int
  def freeze(): EBATable[V]
  def rewrite(mapping: V => V): GenericRefTableBuilder[V]
}

object GenericRefTableBuilder {
  def apply[V: ClassTag](dedup: Boolean)(implicit debugTableName: DebugTableName[V]): GenericRefTableBuilder[V] = {
    if (dedup) {
      val reverse = mutable.HashMap.empty[V, RefVal]
      new DeduplicatingRefTableBuilder[V](debugTableName.tableName, reverse, 0)
    } else {
      val content = mutable.ListBuffer.empty[V]
      new QuickRefTableBuilder[V](debugTableName.tableName, content, 0)
    }
  }
}
