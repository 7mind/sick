package izumi.sick.eba.cursor

import scala.scalajs.js
import scala.scalajs.js.annotation.{JSExportAll, JSExportTopLevel}

@JSExportTopLevel("SickCursor")
@JSExportAll
class SickCursorJs(cursor: SickCursor) {
  def downField(field: String): ObjectCursorJs = {
    new ObjectCursorJs(cursor.downField(field))
  }

  def downArray: ArrayCursorJs = {
    new ArrayCursorJs(cursor.downArray)
  }

  def asNul: Null = cursor.asNul

  def asBool: Boolean = cursor.asBool

  def asByte: Byte = cursor.asByte

  def asShort: Short = cursor.asShort

  def asInt: Int = cursor.asInt

  def asLong: Long = cursor.asLong

  def asBigInt: js.BigInt = js.BigInt(cursor.asBigInt.toString)

  def asFloat: Float = cursor.asFloat

  def asDouble: Double = cursor.asDouble

  def asString: String = cursor.asString

// todo
//  def asArray: Arr = cursor.asArray
//
//  def asObject: Obj = cursor.asObject
//
//  def asRoot: Root = cursor.asRoot
}
