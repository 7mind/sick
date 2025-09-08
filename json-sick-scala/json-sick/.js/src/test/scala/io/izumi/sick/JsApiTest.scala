package io.izumi.sick

import io.circe.Json
import izumi.sick.jsapi.SickJsAPI
import org.scalatest.wordspec.AnyWordSpec

import scala.scalajs.js

class JsApiTest extends AnyWordSpec {

  "test SICK API" in {
    val uint8Array1 = SickJsAPI.encodeObjsToSickUint8Array(js.Dictionary("root1" -> js.Dynamic.literal(a = 1), "root2" -> js.Dynamic.literal(b = 2)))
    val jsAny1 = SickJsAPI.decodeSickUint8Array(uint8Array1)
    val circeJson1 = io.circe.scalajs.convertJsToJson(jsAny1).toTry.get
    assert(circeJson1 == Json.obj("root1" -> Json.obj("a" -> Json.fromInt(1)), "root2" -> Json.obj("b" -> Json.fromInt(2))))

    val uint8Array2 = SickJsAPI.encodeJSONStringsToSickUint8Array(js.Dictionary("root1" -> """{ "a": 2 }""", "root2" -> """{ "b": 3 }"""))
    val jsAny2 = SickJsAPI.decodeSickUint8Array(uint8Array2)
    val circeJson2 = io.circe.scalajs.convertJsToJson(jsAny2).toTry.get
    assert(circeJson2 == Json.obj("root1" -> Json.obj("a" -> Json.fromInt(2)), "root2" -> Json.obj("b" -> Json.fromInt(3))))
  }

}
