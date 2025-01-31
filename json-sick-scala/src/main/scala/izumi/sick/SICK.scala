package izumi.sick

import io.circe.Json
import izumi.sick.SICK.EBA
import izumi.sick.eba.builder.EBABuilder
import izumi.sick.eba.{EBAStructure, SICKSettings}
import izumi.sick.model.Ref
import izumi.sick.sickcirce.CirceTraverser.RWIndexExt

trait SICK {
  def packJson(json: Json, name: String, dedup: Boolean, settings: SICKSettings = SICKSettings.default): EBA = {
    val rwIndex = EBABuilder(dedup)
    val root = rwIndex.append(name, json)
    val structure = rwIndex.freeze(settings)
    EBA(structure, root, rwIndex)
  }
}

object SICK {
  case class EBA(index: EBAStructure, root: Ref, source: EBABuilder)
  object Default extends SICK {}
}
