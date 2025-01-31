package izumi.sick.eba.writer

final case class EBAInfo(version: Int, headerLen: Int, offsets: Seq[Int], length: Long)
