package izumi.sick.eba.cursor

import scala.scalajs.js.annotation.{JSExportAll, JSExportTopLevel}

import scala.scalajs.js
import scala.scalajs.js.JSConverters.*

@JSExportTopLevel("TopCursor")
@JSExportAll
class TopCursorJs(cursor: TopCursor) extends SickCursorJs(cursor.asInstanceOf[SickCursor]) {
  def query(request: String): ObjectCursorJs = {
    new ObjectCursorJs(cursor.query(request))
  }

  def getValues: js.Map[String, ObjectCursorJs] = {
    cursor.getValues.view.mapValues(new ObjectCursorJs(_)).toMap.toJSMap
  }

  def readKey(index: Int): ObjectCursorJs = {
    new ObjectCursorJs(cursor.readKey(index))
  }
}
