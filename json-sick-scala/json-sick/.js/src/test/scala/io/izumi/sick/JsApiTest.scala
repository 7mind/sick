package io.izumi.sick

import io.circe.{Json, Printer}
import izumi.sick.jsapi.{SickJsAPI, bytesToUint8Array}
import org.scalatest.wordspec.AnyWordSpec

import scala.scalajs.js

class JsApiTest extends AnyWordSpec {

  "test SICK API" in {
    locally {
      val uint8Array = SickJsAPI.encodeObjsToSickUint8Array(js.Dictionary("root1" -> js.Dynamic.literal(a = 1), "root2" -> js.Dynamic.literal(b = 2)))
      val jsAny = SickJsAPI.decodeSickUint8Array(uint8Array)
      val circeJson = io.circe.scalajs.convertJsToJson(jsAny).toTry.get
      assert(circeJson == Json.obj("root1" -> Json.obj("a" -> Json.fromInt(1)), "root2" -> Json.obj("b" -> Json.fromInt(2))))
    }

    locally {
      val uint8Array = SickJsAPI.encodeJSONStringsToSickUint8Array(js.Dictionary("root1" -> """{ "a": 2 }""", "root2" -> """{ "b": 3 }"""))
      val jsAny = SickJsAPI.decodeSickUint8Array(uint8Array)
      val circeJson = io.circe.scalajs.convertJsToJson(jsAny).toTry.get
      assert(circeJson == Json.obj("root1" -> Json.obj("a" -> Json.fromInt(2)), "root2" -> Json.obj("b" -> Json.fromInt(3))))
    }

    locally {
      val bytes1 = bytesToUint8Array(Printer.noSpaces.printToByteBuffer(Json.obj("a" -> Json.fromInt(2))).array())
      val bytes2 = bytesToUint8Array(Printer.noSpaces.printToByteBuffer(Json.obj("b" -> Json.fromInt(3))).array())
      val uint8Array = SickJsAPI.encodeJSONBytesToSickUint8Array(js.Dictionary("root1" -> bytes1, "root2" -> bytes2))
      val jsAny = SickJsAPI.decodeSickUint8Array(uint8Array)
      val circeJson = io.circe.scalajs.convertJsToJson(jsAny).toTry.get
      assert(circeJson == Json.obj("root1" -> Json.obj("a" -> Json.fromInt(2)), "root2" -> Json.obj("b" -> Json.fromInt(3))))
    }

    locally {
      val uint8Array = SickJsAPI.encodeObjsToSickUint8Array(js.Dictionary("data" -> js.Dynamic.literal(a = 1, b = 2, c = 3)))
      val cursor = SickJsAPI.sickCursorFromUint8Array(uint8Array, "data")
      assert(cursor.downField("a").asInt.toOption.contains(1))
      assert(cursor.downField("b").asInt.toOption.contains(2))
      assert(cursor.downField("c").asInt.toOption.contains(3))
    }
  }

}
