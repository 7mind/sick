package izumi.sick.eba.cursor

import scala.scalajs.js.annotation.{JSExportAll, JSExportTopLevel}

@JSExportTopLevel("ArrayCursor")
@JSExportAll
class ArrayCursorJs(cursor: ArrayCursor) extends SickCursorJs(cursor.asInstanceOf[SickCursor]) {

  def left: ArrayCursorJs = {
    new ArrayCursorJs(cursor.left)
  }

  def right: ArrayCursorJs = {
    new ArrayCursorJs(cursor.right)
  }

  def value: SickCursorJs = downIndex(cursor.index)

  def downIndex(index: Int): SickCursorJs = {
    new SickCursorJs(cursor.downIndex(index))
  }
}
