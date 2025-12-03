package izumi.sick.jsapi

import io.circe.Json
import izumi.sick.SICK
import izumi.sick.eba.SICKSettings
import izumi.sick.eba.cursor.TopCursorJs
import izumi.sick.eba.reader.{EBAReaderJs, EagerEBAReader, IncrementalEBAReader}
import izumi.sick.eba.writer.EBAWriter
import izumi.sick.model.{SICKWriterParameters, TableWriteStrategy}
import izumi.sick.sickcirce.CirceTraverser.*

import scala.scalajs.js
import scala.scalajs.js.JSConverters.*
import scala.scalajs.js.annotation.JSExportTopLevel
import scala.scalajs.js.typedarray.*

object SickJsAPI {
  /**
    * Accepts an instance of `Uint8Array`, returns a dictionary where keys are root names and values are JSON
    *
    * `decodeSickUint8Array(uint8Array) => { data: { a: 2, b: { c: 3 }, ...etc } }`
    */
  @JSExportTopLevel("decodeSickUint8Array")
  def decodeSickUint8Array(uint8Array: Uint8Array): js.Dictionary[js.Any] = {
    val roIndex = EagerEBAReader.readEBABytes(uint8ArrayToBytes(uint8Array))
    roIndex.roots.asIterable
      .map {
        root =>
          val rootName = roIndex.strings(root.id)
          val json = roIndex.reconstruct(root.ref)
          rootName -> io.circe.scalajs.convertJsonToJs(json)
      }.toMap.toJSDictionary
  }

  /**
    * Accepts a rootName and a JS object (all values should be valid JSON), returns a SICK-encoded binary Uint8Array
    *
    * `encodeObjToSickUint8Array("data", { a: 2, b: { c: 3 }, ...etc }) => Uint8Array`
    */
  @JSExportTopLevel("encodeObjToSickUint8Array")
  def encodeObjToSickUint8Array(rootName: String, obj: js.Any): Uint8Array = {
    encodeObjsToSickUint8Array(js.Dictionary(rootName -> obj))
  }

  /**
    * Accepts dictionary where keys are root names and values are JS objects (all values should be valid JSON), returns a SICK-encoded binary Uint8Array
    *
    * `encodeObjsToSickUint8Array({ data: { a: 2 }, data1: { b: 3 }, ...etc }) => Uint8Array`
    */
  @JSExportTopLevel("encodeObjsToSickUint8Array")
  def encodeObjsToSickUint8Array(objs: js.Dictionary[js.Any]): Uint8Array = {
    encodeToSickUint8ArrayImpl(objs)(io.circe.scalajs.convertJsToJson(_).toTry.get)
  }

  /**
    * Accepts dictionary where keys are root names and values are strings that parse into a valid JSON object (e.g. results of JSON.stringify), returns a SICK-encoded binary Uint8Array
    *
    * `encodeJSONStringsToSickUint8Array({ data: '{ "a": 1 }'}) => Uint8Array`
    */
  @JSExportTopLevel("encodeJSONStringsToSickUint8Array")
  def encodeJSONStringsToSickUint8Array(objs: js.Dictionary[String]): Uint8Array = {
    encodeToSickUint8ArrayImpl(objs)(io.circe.jawn.parse(_).toTry.get)
  }

  /**
    * Accepts dictionary where keys are root names and values are Uint8Arrays containing valid UTF-8 text that parse into JSON, returns a SICK-encoded binary Uint8Array
    *
    * `encodeJSONBytesToSickUint8Array({ data: new Uint8Array(file.buffer)}) => Uint8Array`
    */
  @JSExportTopLevel("encodeJSONBytesToSickUint8Array")
  def encodeJSONBytesToSickUint8Array(objs: js.Dictionary[Uint8Array]): Uint8Array = {
    encodeToSickUint8ArrayImpl(objs) {
      uint8Array =>
        io.circe.jawn.parseByteArray(uint8ArrayToBytes(uint8Array)).toTry.get
    }
  }

  private def encodeToSickUint8ArrayImpl[A](objs: js.Dictionary[A])(f: A => Json): Uint8Array = {
    val jsons = objs.iterator.map {
      case (rootName, a) =>
        rootName -> f(a)
    }.toMap
    val roIndex = SICK.packJsons(jsons, dedup = false, dedupPrimitives = false, avoidBigDecimals = false, SICKSettings.default).index
    val res = EBAWriter.writeBytes(roIndex, SICKWriterParameters(TableWriteStrategy.SinglePassInMemory))
    val bytes = res._1.toArrayUnsafe()
    bytesToUint8Array(bytes)
  }

  /**
   * Accepts an instance of `Uint8Array` and the rootId, returns a cursor to navigate through the structure
   *
   * `{ data: { a: 2, b: { c: 3 } } }`
   * `const cursor = sickCursorFromUint8Array(uint8Array, "data")`
   * `cursor.downField("b").downField("c").asInt`
   */
  @JSExportTopLevel("sickCursorFromUint8Array")
  def sickCursorFromUint8Array(uint8Array: Uint8Array, rootId: String): TopCursorJs = {
    val ebaReader = IncrementalEBAReader.openBytes(uint8ArrayToBytes(uint8Array), eagerOffsets = false)
    new TopCursorJs(ebaReader.getCursor(rootId))
  }

  /**
   * Alternative method for navigating the sick structure, which has query method with jq like requests
   *
   * `{ data: { a: 2, b: { c: [1, 2, 3] } } }`
   * `const reader = ebaReaderFromUint8Array(uint8Array, "data")`
   * `reader.query("b.c.[1]")`
   */
  @JSExportTopLevel("ebaReaderFromUint8Array")
  def ebaReaderFromUint8Array(uint8Array: Uint8Array, rootId: String): EBAReaderJs = {
    val ebaReader = IncrementalEBAReader.openBytes(uint8ArrayToBytes(uint8Array), eagerOffsets = false)
    new EBAReaderJs(ebaReader, rootId)
  }
}
