package izumi.sick.eba.cursor

import izumi.sick.eba.reader.IncrementalEBAReader
import izumi.sick.model.Ref

class TopCursor(val ref: Ref, val ebaReader: IncrementalEBAReader) extends SickCursor
