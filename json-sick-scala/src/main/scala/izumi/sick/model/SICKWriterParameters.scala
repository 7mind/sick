package izumi.sick.model

sealed trait ArrayWriteStrategy

object ArrayWriteStrategy {
  case object StreamRepositioning extends ArrayWriteStrategy
  case object SinglePassInMemory extends ArrayWriteStrategy
  case object DoublePass extends ArrayWriteStrategy
}


case class SICKWriterParameters(arrayWriteStrategy: ArrayWriteStrategy = ArrayWriteStrategy.StreamRepositioning)
