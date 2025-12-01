package izumi.sick.eba.cursor

import scala.scalajs.js
import scala.scalajs.js.annotation.{JSExportAll, JSExportTopLevel}
import scala.scalajs.js.JSConverters._

@JSExportTopLevel("SickCursor")
@JSExportAll
class SickCursorJs(cursor: SickCursor) {
  def downField(field: String): ObjectCursorJs = {
    new ObjectCursorJs(cursor.downField(field))
  }

  def downArray: ArrayCursorJs = {
    new ArrayCursorJs(cursor.downArray)
  }

  def asNul: js.UndefOr[Null] = cursor.asNul.orUndefined

  def asBool: js.UndefOr[Boolean] = cursor.asBool.orUndefined

  def asByte: js.UndefOr[Byte] = cursor.asByte.orUndefined

  def asShort: js.UndefOr[Short] = cursor.asShort.orUndefined

  def asInt: js.UndefOr[Int] = cursor.asInt.orUndefined

  def asLong: js.UndefOr[Long] = cursor.asLong.orUndefined

  def asBigInt: js.UndefOr[js.BigInt] = cursor.asBigInt.map(v => js.BigInt(v.toString)).orUndefined

  def asFloat: js.UndefOr[Float] = cursor.asFloat.orUndefined

  def asDouble: js.UndefOr[Double] = cursor.asDouble.orUndefined

  def asString: js.UndefOr[String] = cursor.asString.orUndefined
}
