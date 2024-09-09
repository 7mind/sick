[![Build](https://github.com/7mind/sick/workflows/Build/badge.svg)](https://github.com/7mind/sick/actions/workflows/build.yml)
[![Latest Release](https://img.shields.io/github/tag/7mind/sick.svg)](https://github.com/7mind/sick/releases)
[![Maven Central](https://img.shields.io/maven-central/v/io.7mind.sick/sick_2.13.svg)](http://search.maven.org/#search%7Cga%7C1%7Cg%3A%22io.7mind.sick%22)
[![Latest version](https://index.scala-lang.org/7mind/sick/latest.svg?color=orange)](https://index.scala-lang.org/7mind/sick)

# SICK: Streams of Independent Constant Keys

`SICK` is an approach to handle `JSON`-like structures and various libraries implementing it.

`SICK` allows you to achieve the following:

1. Store `JSON`-like data in efficient indexed binary form
2. Avoid reading and parsing whole `JSON` files and access only the data you need just in time
3. Store multiple `JSON`-like structures in one deduplicating storage
4. Implement perfect streaming parsers for `JSON`-like data
5. Efficiently stream updates for `JSON`-like data

The tradeoff for these benefits is somehow more complicated and less efficient encoder.

## The problem

`JSON` has a [Type-2](https://en.wikipedia.org/wiki/Chomsky_hierarchy#Type-2_grammars) grammar and requires a [pushdown automaton](https://en.wikipedia.org/wiki/Pushdown_automaton) to parse it. So, it's not possible to implement efficient streaming parser for `JSON`. Just imagine a huge hierarchy of nested `JSON` objects: you won't be able to finish parsing the top-level object until you process the whole file.

`JSON` is frequently used to store and transfer large amounts of data and these transfers tend to [grow over time](https://nee.lv/2021/02/28/How-I-cut-GTA-Online-loading-times-by-70/). Just imagine a typical `JSON` config file for a large enterprise product.

The non-streaming nature of almost all the JSON parsers requires a lot of work to be done every time you need to deserialize a huge chunk of `JSON` data: you need to read it from disk, parse it in memory into an AST representation, and, usually, map raw `JSON` tree to object instances. Even if you use token streams and know the type of your object ahead of time you still have to deal with the Type-2 grammar.

This may be very inefficient and causes unnecessary delays, pauses, CPU activity and memory consumption spikes.

## The idea

Let's assume that we have a small `JSON`:

```json
[
    {"some key": "some value"},
    {"some key": "some value"},
    {"some value": "some key"},
]
```

Let's build a table for every unique value in our `JSON` :


| Type   | index | Value                          | Is Root         |
| ------ | ----- | ------------------------------ | --------------- |
| string | 0     | "some key"                     | No              |
| string | 1     | "some value"                   | No              |
| object | 0     | [string:0, string:1]           | No              |
| object | 1     | [string:1, string:0]           | No              |
| array  | 0     | [object:0, object:0, object:1] | Yes (file.json) |

We just built a flattened and deduplicated version of our initial `JSON` structure.

### Streaming

Such representation allows us to do many different things, for example we may stream our table:

```
string:0 = "some key"
string:1 = "some value"

object:0.size = 2
object:0[string:0] = string:1
object:1[string:1] = string:0

array:0.size = 2
array:0[0] = object:0
array:0[1] = object:1

string:2 = "file.json"

root:0=array:0,string:2
```

This particular encoding is inefficient but it's streamable and, moreover, we can add removal message into it thus supporting arbitrary updates:

```
array:0[0] = object:1
array:0[1] = remove
```

There is an interesting observation: when a stream does not contain removal entries it can be safely reordered. Unfortunately, in some usecases the receiver still may need to accumulate the entries in a buffer until it can sort them out.

### Binary format: EBA (Efficient Binary Aggregate)

We may note that the only complex data structures in our "Value" column are lists and `(type, index)` pairs. Let's call such pairs "references".

A reference can be represented as a pair of integers, so it would have a fixed byte length.

A list of references can be represented as an integer storing list length followed by all the references in their binary form. Let's note that such binary structure is indexed, once we know the index of an element we want to access we can do it immediately.

A list of any fixed-size scalar values can be represented the same way.

A list of variable-size values (e.g. a list of strings) can be represented the following way:

```
  {strings count}{list of string offsets}{all the strings concatenated}
```

So, `["a", "bb", "ccc"]` would become something like `3 0 2 3 a b bb ccc` without spaces.

An important fact is that this encoding is indexed too and it can be reused to store any lists of variable-length data.

#### EBA Structures

TODO: explain the overall EBA structure format, including tables, etc

### Additional capabilities over `JSON`

`SICK` encoding follows compositional principles of `JSON` (a set primitive types plus lists and dictionaries), though it is more powerful: it has "reference" type and allows you to encode custom types.

(1) It's easy to note that our table may store circular references, something `JSON` can't do natively:

| Type   | index | Value                | Is Root |
| ------ | ----- | -------------------- | ------- |
| object | 0     | [string:0, object:1] | No      |
| object | 1     | [string:1, object:0] | No      |

This may be convenient in some complex cases.

(2) Also we may note, that we may happily store multiple json files in one table and have full deduplication over their content. We just need to introduce a separate attribute (`is root`) storing either nothing or the name of our "root entry" (`JSON` file).

In real implementation it's more convenient to just create a separate "root" type, the value of a root type should always be a reference to its name and a reference to the actual `JSON` value we encoded:

| Type   | index | Value                          |
| ------ | ----- | ------------------------------ |
| string | 0     | "some key"                     |
| string | 1     | "some value"                   |
| string | 2     | "some value"                   |
| object | 0     | [string:0, string,1]           |
| object | 1     | [string:1, string:0]           |
| array  | 0     | [object:0, object:0, object:1] |
| root   | 0     | [string:2, array:0]            |

(3) We may encode custom scalar data types (e.g. timestamps) natively just by introducing new type tags.

(4) We may even store polymorphic types by introducing new type tags or even new type references.

## Implementation

Currently we provide C# and Scala implementations of SICK indexed binary JSON storage. Currently the code in this repository has no streaming capabilities. That may change in the future. It's not a hard problem to add streaming support, your contributions are welcome.

| Language | Binary Storage Encoder |  Binary Storage Decoder | Stream Encoder |  Stream Decoder | Encoder AST   | Decoder AST |
| -------- | ---------------------- | ----------------------- | -------------- | --------------- | ------------  | ----------- |
| Scala    | Yes                    | No                      | No             | No              | Circe         | N/A         |
| C#       | Yes                    | Yes                     | No             | No              | JSON.Net      | Custom      |

#### Supported types

A type marker is represented as a single-byte unsigned integer. The possible values are:

| Marker | Name    | Comment                        | Value Length (bytes)      | C# mapping | Scala Mapping |
| ------ | ------- | ------------------------------ | ------------------------- | ---------- | --------------|
| 0      | TNul    | Equivalent to `null` in JSON   | 4, stored in the marker   |            |               |
| 1      | TBit    | Boolean                        | 4, stored in the marker   |            |               |
| 2      | TByte   | Byte,                          | 4, stored in the marker   | byte (unsigned)| Byte (signed)               |
| 3      | TShort  | Signed 16-bit integer          | 4, stored in the marker   |           |               |
| 4      | TInt    | Signed 32-bit integer          | 4                         |           |               |
| 5      | TLng    | Signed 64-bit integer          | 8                         |           |               |
| 6      | TBigInt |                                | Variable, prefixed        |           |               |
| 7      | TDbl    |                                | 8                         |           |               |
| 8      | TFlt    |                                | 4                         |           |               |
| 9      | TBigDec |                                | Variable, prefixed        | Custom: scale/precision/signum/unscaled quadruple in C# | |
| 10     | TStr    | UTF-8 String                   | Variable, prefixed        |           |               |
| 11     | TArr    | List of array entries          | Variable, prefixed        |           |               |
| 12     | TObj    | List of object entries         | Variable, prefixed        |           |               |
| 15     | TRoot   | Index of the name string (4 bytes) + reference (4+1=5 bytes) | 9          |           |               |

#### References

TODO

#### Lists

TODO

#### Array entries

Array entries are just references.

#### Object entries

TODO

#### Object entry skip list and KHash

TODO

#### Value tables

TODO

### Limitations

Current implementation has the following limitations:

1. Maximum object size: `65534` keys
2. The order of object keys is not preserved
3. Maximum amount of array elements: `2^32`
4. Maximum amount of unique values of the same type: `2^32`

These limitations may be lifted by using more bytes to store offset pointers and counts on binary level.
Though it's hard to imagine a real application which would need that.
