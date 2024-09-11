package izumi.sick

import io.circe.Json
import izumi.sick.SICK.EBA
import izumi.sick.eba.builder.EBABuilder
import izumi.sick.eba.{EBAStructure, SICKSettings}
import izumi.sick.model.Ref
import izumi.sick.sickcirce.CirceTraverser.RWIndexExt

trait SICK {
  def pack(json: Json, name: String, dedup: Boolean, settings: SICKSettings = SICKSettings.default): EBA = {
    val rwIndex = EBABuilder(dedup = dedup)
    val root = rwIndex.append(name, json)
    EBA(rwIndex.freeze(settings), root, rwIndex)
  }
}

object SICK {
  case class EBA(index: EBAStructure, root: Ref, source: EBABuilder)
  object Default extends SICK {}
}
