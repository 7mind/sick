# @izumi-framework/json-sick

SICK - High-performance binary storage for JSON-like data structures.

Visit the [SICK GitHub repository](https://github.com/7mind/sick) for more information.

## Installation

```bash
npm install @izumi-framework/json-sick
```

## Usage

```javascript
const {
  decodeSickUint8Array,
  encodeObjToSickUint8Array,
  encodeObjsToSickUint8Array,
  encodeJSONStringsToSickUint8Array,
  encodeJSONBytesToSickUint8Array
} = require('@izumi-framework/json-sick');

// Encode a single object
const encoded = encodeObjToSickUint8Array("data", { a: 2, b: { c: 3 } });

// Encode multiple objects
const multiEncoded = encodeObjsToSickUint8Array({
  data: { a: 2 },
  data1: { b: 3 }
});

// Decode
const decoded = decodeSickUint8Array(encoded);
console.log(decoded); // { data: { a: 2, b: { c: 3 } } }
```

One-liner:

```bash
npm install @izumi-framework/json-sick && node -e "
  const { encodeObjToSickUint8Array, decodeSickUint8Array } = require('@izumi-framework/json-sick');
  const encoded = encodeObjToSickUint8Array('data', { a: 2, b: { c: 3 } });
  console.log(decodeSickUint8Array(encoded));
  "
```

## API

### `decodeSickUint8Array(uint8Array: Uint8Array): {[key: string]: any}`

Accepts an instance of `Uint8Array`, returns a dictionary where keys are root names and values are JSON.

### `encodeObjToSickUint8Array(rootName: string, obj: any): Uint8Array`

Accepts a rootName and a JS object (all values should be valid JSON), returns a SICK-encoded binary Uint8Array.

### `encodeObjsToSickUint8Array(objs: {[key: string]: any}): Uint8Array`

Accepts dictionary where keys are root names and values are JS objects (all values should be valid JSON), returns a SICK-encoded binary Uint8Array.

### `encodeJSONStringsToSickUint8Array(objs: {[key: string]: string}): Uint8Array`

Accepts dictionary where keys are root names and values are strings that parse into a valid JSON object (e.g. results of JSON.stringify), returns a SICK-encoded binary Uint8Array.

### `encodeJSONBytesToSickUint8Array(objs: {[key: string]: Uint8Array}): Uint8Array`

Accepts dictionary where keys are root names and values are Uint8Arrays containing valid UTF-8 text that parse into JSON, returns a SICK-encoded binary Uint8Array.

### `sickCursorFromUint8Array(uint8Array: Uint8Array, rootId: string): TopCursor`

Accepts an instance of `Uint8Array` and the rootId, returns a cursor to navigate through the structure.

### `ebaReaderFromUint8Array(uint8Array: Uint8Array, rootId: string): EBAReader`

Accepts an instance of `Uint8Array` and the rootId, returns a reader, which has query method, with jq like requests.

## License

BSD-2-Clause

