/**
 * Accepts an instance of `Uint8Array`, returns a dictionary where keys are root names and values are JSON
 *
 * @example
 * decodeSickUint8Array(uint8Array) // => { data: { a: 2, b: { c: 3 }, ...etc } }
 */
export function decodeSickUint8Array(uint8Array: Uint8Array): {[key: string]: any};

/**
 * Accepts a rootName and a JS object (all values should be valid JSON), returns a SICK-encoded binary Uint8Array
 *
 * @example
 * encodeObjToSickUint8Array("data", { a: 2, b: { c: 3 }, ...etc }) // => Uint8Array
 */
export function encodeObjToSickUint8Array(rootName: string, obj: any): Uint8Array;

/**
 * Accepts dictionary where keys are root names and values are JS objects (all values should be valid JSON), returns a SICK-encoded binary Uint8Array
 *
 * @example
 * encodeObjsToSickUint8Array({ data: { a: 2 }, data1: { b: 3 }, ...etc }) // => Uint8Array
 */
export function encodeObjsToSickUint8Array(objs: {[key: string]: any}): Uint8Array;

/**
 * Accepts dictionary where keys are root names and values are strings that parse into a valid JSON object (e.g. results of JSON.stringify), returns a SICK-encoded binary Uint8Array
 *
 * @example
 * encodeJSONStringsToSickUint8Array({ data: '{ "a": 1 }'}) // => Uint8Array
 */
export function encodeJSONStringsToSickUint8Array(objs: {[key: string]: string}): Uint8Array;

/**
 * Accepts dictionary where keys are root names and values are Uint8Arrays containing valid UTF-8 text that parse into JSON, returns a SICK-encoded binary Uint8Array
 *
 * @example
 * encodeJSONBytesToSickUint8Array({ data: new Uint8Array(file.buffer)}) // => Uint8Array
 */
export function encodeJSONBytesToSickUint8Array(objs: {[key: string]: Uint8Array}): Uint8Array;
