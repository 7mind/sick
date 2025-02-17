package izumi.sick

import io.circe.Json
import izumi.sick.eba.builder.EBABuilder
import izumi.sick.eba.reader.{EagerEBAReader, IncrementalEBAReader}
import izumi.sick.eba.writer.EBAWriter
import izumi.sick.eba.{EBAStructure, SICKSettings}
import izumi.sick.model.Ref
import izumi.sick.sickcirce.CirceTraverser.RWIndexExt

object SICK {
  def packJson(json: Json, name: String, dedup: Boolean, settings: SICKSettings = SICKSettings.default): EBA = {
    val rwIndex = EBABuilder(dedup)
    val root = rwIndex.append(name, json)
    val structure = rwIndex.freeze(settings)
    EBA(structure, root, rwIndex)
  }

  final val writer: EBAWriter.type = EBAWriter

  final val incrementalReader: IncrementalEBAReader.type = IncrementalEBAReader

  final val eagerReader: EagerEBAReader.type = EagerEBAReader

  final case class EBA(index: EBAStructure, root: Ref, source: EBABuilder)
}
