package io.izumi.sick

import io.circe.jawn.parse
import izumi.sick.SICK
import izumi.sick.eba.reader.IncrementalEBAReader
import izumi.sick.eba.writer.EBAWriter
import izumi.sick.model.{SICKWriterParameters, TableWriteStrategy}
import org.scalatest.wordspec.AnyWordSpec

class SickCursorTest extends AnyWordSpec {

  "cursor simple test" in {
    val jsonString = """{"data": {"name": "Alice", "age": 30, "city": "NYC"}}"""
    val json = parse(jsonString).toTry.get

    val eba = SICK.packJson(
      json = json,
      name = "user.json",
      dedup = true,
      dedupPrimitives = true,
      avoidBigDecimals = false
    )

    val (bytes, _) = EBAWriter.writeBytes(
      eba.index,
      SICKWriterParameters(TableWriteStrategy.SinglePassInMemory)
    )
    val bytesArray = bytes.toArrayUnsafe()
    val reader = IncrementalEBAReader.openBytes(bytesArray, eagerOffsets = false)
    val cursor = reader.getCursor(eba.root)

    assert(cursor.downField("data").downField("name").asString == "Alice")
    assert(cursor.downField("data").downField("age").asByte == 30)
    assert(cursor.downField("data").downField("city").asString == "NYC")
  }

  "cursor support various kinds" in {
    val jsonString =
      """{
        |  "null": null,
        |  "bit": true,
        |  "byte": 42,
        |  "short": 30000,
        |  "int": 2000000000,
        |  "long": 9000000000000000000,
        |  "bigint": 123456789012345678901234567890,
        |  "float": 3.14,
        |  "double": 42.4242424242,
        |  "bigdec": 123.45678901234567890,
        |  "string": "test",
        |  "object": {
        |    "field": "test field"
        |  }
        |}""".stripMargin
    val json = parse(jsonString).toTry.get

    val eba = SICK.packJson(
      json = json,
      name = "user.json",
      dedup = true,
      dedupPrimitives = true,
      avoidBigDecimals = false
    )

    val (bytes, _) = EBAWriter.writeBytes(
      eba.index,
      SICKWriterParameters(TableWriteStrategy.SinglePassInMemory)
    )
    val bytesArray = bytes.toArrayUnsafe()
    val reader = IncrementalEBAReader.openBytes(bytesArray, eagerOffsets = false)
    val cursor = reader.getCursor(eba.root)
    cursor.downField("null").asNul
    assert(cursor.downField("bit").asBool)
    assert(cursor.downField("byte").asByte == 42)
    assert(cursor.downField("short").asShort == 30000)
    assert(cursor.downField("int").asInt == 2000000000)
    assert(cursor.downField("int").asLong == 2000000000L)
    assert(cursor.downField("long").asLong == 9000000000000000000L)
    assert(cursor.downField("bigint").asBigInt == BigInt.apply("123456789012345678901234567890"))
    assert(cursor.downField("float").asFloat == 3.14.toFloat)
    assert(cursor.downField("float").asDouble == 3.14f.toDouble)
    assert(cursor.downField("double").asDouble == 42.4242424242d)
    assert(cursor.downField("bigdec").asBigDec == BigDecimal.apply("123.45678901234567890"))
    assert(cursor.downField("string").asString == "test")
    assert(cursor.downField("object").downField("field").asString == "test field")
  }

  "cursor support arrays" in {
    val jsonString =
      """{
        |  "array" : [
        |     "one",
        |     "two",
        |     "three"
        |  ],
        |  "objects" : [
        |     {"name": "Alice", "age": 30, "city": "NYC"},
        |     {"name": "Bob", "age": 42, "city": "Chicago"}
        |  ]
        |}""".stripMargin
    val json = parse(jsonString).toTry.get

    val eba = SICK.packJson(
      json = json,
      name = "user.json",
      dedup = true,
      dedupPrimitives = true,
      avoidBigDecimals = false
    )

    val (bytes, _) = EBAWriter.writeBytes(
      eba.index,
      SICKWriterParameters(TableWriteStrategy.SinglePassInMemory)
    )
    val bytesArray = bytes.toArrayUnsafe()
    val reader = IncrementalEBAReader.openBytes(bytesArray, eagerOffsets = false)
    val cursor = reader.getCursor(eba.root)
    assert(cursor.downField("array").downArray.downIndex(0).asString == "one")
    assert(cursor.downField("objects").downArray.downIndex(1).downField("name").asString == "Bob")

    val arrayCursor = cursor.downField("array").downArray

    assert(arrayCursor.value.asString == "one")
    assert(arrayCursor.right.value.asString == "two")
    assert(arrayCursor.right.right.value.asString == "three")
    assert(arrayCursor.right.left.value.asString == "one")
  }

//  "cursor support queries" in {
//    val jsonString =
//      """{
//        |  "data": {
//        |     "person": {"name": "Alice", "age": 30, "city": "NYC"}
//        |  }
//        |}""".stripMargin
//    val json = parse(jsonString).toTry.get
//
//    val eba = SICK.packJson(
//      json = json,
//      name = "user.json",
//      dedup = true,
//      dedupPrimitives = true,
//      avoidBigDecimals = false
//    )
//
//    val (bytes, _) = EBAWriter.writeBytes(
//      eba.index,
//      SICKWriterParameters(TableWriteStrategy.SinglePassInMemory)
//    )
//    val bytesArray = bytes.toArrayUnsafe()
//    val reader = IncrementalEBAReader.openBytes(bytesArray, eagerOffsets = false)
//    val cursor = reader.getCursor(eba.root)
//    println(cursor.query("data.person.name").asObject)
//  }
}
