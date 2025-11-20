package izumi.sick.eba.cursor

import izumi.sick.eba.reader.IncrementalEBAReader
import izumi.sick.model.Ref

class TopCursor(val ref: Ref, val ebaReader: IncrementalEBAReader) extends SickCursor {
  def query(request: String): ObjectCursor = {
    new ObjectCursor(ebaReader.queryRef(ref, request)._1, ebaReader)
  }
}
