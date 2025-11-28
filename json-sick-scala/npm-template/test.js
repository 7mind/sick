import test from "ava";

import {
  decodeSickUint8Array,
  encodeObjToSickUint8Array,
  encodeObjsToSickUint8Array,
  encodeJSONStringsToSickUint8Array,
  encodeJSONBytesToSickUint8Array,
  sickCursorFromUint8Array
} from "./json-sick-2.13-fullOpt.js";

test("Encode/Decode obj test", t => {
    const encoded = encodeObjToSickUint8Array("data", { a: 2, b: { c: 3 , d: true} });
    const decoded = decodeSickUint8Array(encoded);
    t.deepEqual(decoded, { data: { a: 2, b: { c: 3 , d: true} } });
})

test("Encode/Decode JSON strings test", t => {
    const encoded = encodeJSONStringsToSickUint8Array({"a": "2", "b": "3"});
    const decoded = decodeSickUint8Array(encoded);
    t.deepEqual(decoded, { a: 2, b: 3 });
})

test("Encode/Decode multiple objects test", t => {
    const multiEncoded = encodeObjsToSickUint8Array({
      data: { a: 2 },
      data1: { b: 3 }
    });
    const decoded = decodeSickUint8Array(multiEncoded);
    t.deepEqual(decoded, { data: { a: 2 }, data1: { b: 3 } });
})

test("Sick Cursors test", t => {
    const encoded = encodeObjToSickUint8Array("data",
        {
            nul: null,
            bit: true,
            byte: 42,
            int: 30000,
            bigint: 123456789012345678901234567890,
            double: 3.14,
            bigdec: 123.45678901234567890,
            string: "test",
            object: {
                life: 42,
                person: { name: "Alice", age: 30, city: "NYC" }
            },
            arr: ["a", "b", "c"]
         });
    const cursor = sickCursorFromUint8Array(encoded, "data");

    t.is(cursor.downField("nul").asNul, null);
    t.true(cursor.downField("bit").asBool);
    t.is(cursor.downField("byte").asByte, 42);
    t.is(cursor.downField("int").asInt, 30000);
    t.is(cursor.downField("bigint").asBigInt, 123456789012345680000000000000n);
    t.is(cursor.downField("double").asDouble, 3.14);
    t.is(cursor.downField("string").asString, "test");
    t.is(cursor.downField("object").downField("life").asInt, 42);

    t.is(cursor.query("object.person.name").asString, "Alice");
    t.is(cursor.downField("object").getValues.get("person").downField("name").asString, "Alice");

    t.is(cursor.downField("arr").downArray.right.value.asString, "b");
    t.is(cursor.downField("arr").downArray.downIndex(2).asString, "c");
})