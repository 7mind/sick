package izumi.sick.eba.cursor

import izumi.sick.eba.reader.IncrementalEBAReader
import izumi.sick.model.Ref
import izumi.sick.model.RefKind.TArr

class ArrayCursor(val ref: Ref, val ebaReader: IncrementalEBAReader, val index: Int = 0) extends SickCursor {
  private val length = ebaReader.arrTable.readElem(ref.ref).length

  def left: ArrayCursor = {
    if (index == 0) throw new ArrayIndexOutOfBoundsException("Can not move left: Index is on first element")
    else new ArrayCursor(ref, ebaReader, index - 1)
  }

  def right: ArrayCursor = {
    if (index == length - 1) throw new ArrayIndexOutOfBoundsException(s"Can not move right: Index is on last element")
    else new ArrayCursor(ref, ebaReader, index + 1)
  }

  def value: SickCursor = downIndex(index)

  def downIndex(index: Int): SickCursor = {
    val newRef = ebaReader.readArrayElementRef(ref, index)
    newRef.kind match {
      case TArr => new ArrayCursor(newRef, ebaReader)
      case _ => new ObjectCursor(newRef, ebaReader)
    }
  }
}
