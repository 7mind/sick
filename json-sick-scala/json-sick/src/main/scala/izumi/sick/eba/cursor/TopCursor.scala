package izumi.sick.eba.cursor

import izumi.sick.eba.reader.IncrementalEBAReader
import izumi.sick.model.Ref
import izumi.sick.model.RefKind.TObj

class TopCursor(val ref: Ref, val ebaReader: IncrementalEBAReader) extends SickCursor {
  def query(request: String): ObjectCursor = {
    new ObjectCursor(ebaReader.queryRef(ref, request)._1, ebaReader)
  }

  def getReferences: Map[String, Ref] = {
    if (ref.kind == TObj) ebaReader.objTable.readElem(ref.ref).iterator.toMap
    else throw new RuntimeException(s"Can not get references for kind ${ref.kind}")
  }

  def getValues: Map[String, ObjectCursor] = {
    getReferences.view.mapValues(ref =>
      new ObjectCursor(ref, ebaReader)
    ).toMap
  }

  def readKey(index: Int): ObjectCursor = {
    if (ref.kind == TObj) new ObjectCursor(ebaReader.objTable.readElem(ref.ref).readKey(index)._2, ebaReader)
    else throw new RuntimeException(s"Can not read key for kind ${ref.kind}")
  }
}
