package izumi.sick.eba.cursor

import izumi.sick.model.Ref

import scala.scalajs.js.annotation.{JSExport, JSExportTopLevel}
import scala.scalajs.js
import scala.scalajs.js.JSConverters.*

@JSExportTopLevel("TopCursor")
class TopCursorJs(cursor: TopCursor) extends SickCursorJs(cursor.asInstanceOf[SickCursor]) {

  @JSExport
  def query(request: String): ObjectCursorJs = {
    new ObjectCursorJs(cursor.query(request))
  }

  @JSExport
  def getValues: js.Map[String, ObjectCursorJs] = {
    cursor.getValues.view.mapValues(new ObjectCursorJs(_)).toMap.toJSMap
  }

  @JSExport
  def readKey(index: Int): ObjectCursorJs = {
    new ObjectCursorJs(cursor.readKey(index))
  }

  protected[eba] def ref: Ref = cursor.ref
}
