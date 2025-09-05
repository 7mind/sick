package izumi.sick.eba

final case class SICKSettings(
  objectIndexBucketCount: Short,
  minObjectKeysBeforeIndexing: Short,
)

object SICKSettings {
  def default: SICKSettings = {
    SICKSettings(
      objectIndexBucketCount = 128,
      minObjectKeysBeforeIndexing = 2,
    )
  }
}
