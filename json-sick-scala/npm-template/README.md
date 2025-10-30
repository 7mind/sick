# @7mind/json-sick

SICK (Structured Interchange and Compression Kit) - High-performance binary JSON encoding format.

## Installation

```bash
npm install @7mind/json-sick
```

## Usage

```javascript
const {
  decodeSickUint8Array,
  encodeObjToSickUint8Array,
  encodeObjsToSickUint8Array,
  encodeJSONStringsToSickUint8Array,
  encodeJSONBytesToSickUint8Array
} = require('@7mind/json-sick');

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

## License

BSD-2-Clause

## More Information

Visit the [SICK GitHub repository](https://github.com/7mind/sick) for more information.
