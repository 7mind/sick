package izumi.sick.eba.reader

import izumi.sick.eba.cursor.{ObjectCursorJs, TopCursorJs}
import izumi.sick.model.RefKind

import scala.scalajs.js
import scala.scalajs.js.annotation.{JSExportAll, JSExportTopLevel}

@JSExportTopLevel("EBAReader")
@JSExportAll
class EBAReaderJs(reader: IncrementalEBAReader, rootId: String) {
  def query(request: String): js.Any = {
    val cursor = new TopCursorJs(reader.getCursor(rootId)).query(request)
    resolveCursorRef(cursor)
  }

  private def resolveCursorRef(cursor: ObjectCursorJs): js.Any = {
    cursor.ref.kind match {
      case RefKind.TNul    => cursor.asNul
      case RefKind.TBit    => cursor.asBool
      case RefKind.TByte   => cursor.asByte
      case RefKind.TShort  => cursor.asShort
      case RefKind.TInt    => cursor.asInt
      case RefKind.TLng    => cursor.asDouble
      case RefKind.TBigInt => cursor.asBigInt
      case RefKind.TFlt    => cursor.asFloat
      case RefKind.TDbl    => cursor.asDouble
      case RefKind.TStr    => cursor.asString
      case _               => js.undefined
    }
  }

}
