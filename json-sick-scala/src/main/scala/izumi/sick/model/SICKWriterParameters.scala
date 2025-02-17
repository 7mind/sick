package izumi.sick.model

sealed trait TableWriteStrategy

object TableWriteStrategy {
  case object StreamRepositioning extends TableWriteStrategy
  case object SinglePassInMemory extends TableWriteStrategy
  case object DoublePass extends TableWriteStrategy
}

final case class SICKWriterParameters(
  tableWriteStrategy: TableWriteStrategy = TableWriteStrategy.StreamRepositioning
)
