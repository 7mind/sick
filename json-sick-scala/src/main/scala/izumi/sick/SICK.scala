package izumi.sick

import io.circe.Json
import izumi.sick.SICK.EBA
import izumi.sick.indexes.{IndexRO, IndexRW, SICKSettings}
import izumi.sick.model.Ref

trait SICK {
  def pack(json: Json, name: String, dedup: Boolean, settings: SICKSettings = SICKSettings.default): EBA = {
    import izumi.sick.sickcirce.CirceTraverser.*
    val rwIndex = IndexRW(dedup = dedup)
    val root = rwIndex.append(name, json)
    EBA(rwIndex.freeze(settings), root, rwIndex)
  }
}

object SICK {
  case class EBA(index: IndexRO, root: Ref, source: IndexRW)
  object Default extends SICK {}
}
