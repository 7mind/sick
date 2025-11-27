package izumi.sick.eba.cursor

import scala.scalajs.js.annotation.{JSExportAll, JSExportTopLevel}

@JSExportTopLevel("ObjectCursor")
@JSExportAll
class ObjectCursorJs(cursor: ObjectCursor) extends TopCursorJs(cursor.asInstanceOf[TopCursor])
