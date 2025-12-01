package izumi.sick.eba.cursor

import izumi.sick.eba.reader.IncrementalEBAReader
import izumi.sick.model.Ref

class ObjectCursor(override val ref: Ref, override val ebaReader: IncrementalEBAReader) extends TopCursor(ref, ebaReader)
