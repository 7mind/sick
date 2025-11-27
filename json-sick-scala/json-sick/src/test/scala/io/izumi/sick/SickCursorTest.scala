package io.izumi.sick

import io.circe.jawn.parse
import izumi.sick.SICK
import izumi.sick.eba.reader.IncrementalEBAReader
import izumi.sick.eba.writer.EBAWriter
import izumi.sick.model.RefKind.TObj
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
    val reader =
      IncrementalEBAReader.openBytes(bytesArray, eagerOffsets = false)
    val cursor = reader.getCursor(eba.root)

    assert(cursor.downField("data").downField("name").asString.contains("Alice"))
    assert(cursor.downField("data").downField("age").asByte.contains(30))
    assert(cursor.downField("data").downField("city").asString.contains("NYC"))
    assert(cursor.downField("data").downField("age").asString.isEmpty)
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
        |  "float": 1.5,
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
    val reader =
      IncrementalEBAReader.openBytes(bytesArray, eagerOffsets = false)
    val cursor = reader.getCursor(eba.root)
    cursor.downField("null").asNul
    assert(cursor.downField("bit").asBool.contains(true))
    assert(cursor.downField("byte").asByte.contains(42))
    assert(cursor.downField("short").asShort.contains(30000))
    assert(cursor.downField("int").asInt.contains(2000000000))
    assert(cursor.downField("int").asLong.contains(2000000000L))
    assert(cursor.downField("long").asLong.contains(9000000000000000000L))
    assert(
      cursor.downField("bigint").asBigInt.contains(BigInt.apply(
        "123456789012345678901234567890"
      ))
    )
    assert(cursor.downField("float").asFloat.contains(1.5.toFloat))
    assert(cursor.downField("float").asDouble.contains(1.5))
    assert(cursor.downField("double").asDouble.contains(42.4242424242d))
    assert(
      cursor.downField("bigdec").asBigDec.contains(BigDecimal.apply(
        "123.45678901234567890"
      ))
    )
    assert(cursor.downField("string").asString.contains("test"))
    assert(cursor.downField("object").downField("field").asString.contains("test field"))
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
    val reader =
      IncrementalEBAReader.openBytes(bytesArray, eagerOffsets = false)
    val cursor = reader.getCursor(eba.root)
    assert(cursor.downField("array").downArray.downIndex(0).asString.contains("one"))
    assert(
      cursor
        .downField("objects")
        .downArray
        .downIndex(1)
        .downField("name")
        .asString.contains("Bob")
    )

    val arrayCursor = cursor.downField("array").downArray

    assert(arrayCursor.value.asString.contains("one"))
    assert(arrayCursor.right.value.asString.contains("two"))
    assert(arrayCursor.right.right.value.asString.contains("three"))
    assert(arrayCursor.right.left.value.asString.contains("one"))
  }

  "cursor support queries" in {
    val jsonString =
      """{
        |  "data": {
        |     "person": {"name": "Alice", "age": 30, "city": "NYC"},
        |     "numbers" : [
        |       "one",
        |       "two",
        |       "three"
        |     ]
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
    val reader =
      IncrementalEBAReader.openBytes(bytesArray, eagerOffsets = false)
    val cursor = reader.getCursor(eba.root)
    assert(cursor.query("data.person").ref.kind == TObj)
    assert(cursor.query("data.person.name").asString.contains("Alice"))

    assert(cursor.query("data.person").getReferences.size == 3)
    assert(cursor.query("data.person").getValues.get("name").exists(_.asString.contains("Alice")))

    assert(cursor.query("data.person").readKey(1).asInt.contains(30))
  }
}
