import test from "ava";

import {
  decodeSickUint8Array,
  encodeObjToSickUint8Array,
  encodeObjsToSickUint8Array,
  encodeJSONStringsToSickUint8Array,
  encodeJSONBytesToSickUint8Array
} from "./json-sick-2.13-fullOpt.js";

test("Encode/Decode obj test", t => {
    const encoded = encodeObjToSickUint8Array("data", { a: 2, b: { c: 3 } });
    const decoded = decodeSickUint8Array(encoded);
    t.deepEqual(decoded, { data: { a: 2, b: { c: 3 } } });
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