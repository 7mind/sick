[![Build](https://github.com/7mind/sick/workflows/Build/badge.svg)](https://github.com/7mind/sick/actions/workflows/build.yml)
[![Latest Release](https://img.shields.io/github/tag/7mind/sick.svg)](https://github.com/7mind/sick/releases)
[![Maven Central](https://img.shields.io/maven-central/v/io.7mind.izumi/json-sick_2.13.svg)](https://central.sonatype.com/search?q=g:io.7mind.izumi+a:json-sick*)
[![NuGet](https://img.shields.io/nuget/v/Izumi.SICK.svg)](https://www.nuget.org/packages/Izumi.SICK/)
[![npm](https://img.shields.io/npm/v/@izumi-framework/json-sick.svg)](https://www.npmjs.com/package/@izumi-framework/json-sick)
[![Latest version](https://index.scala-lang.org/7mind/sick/latest.svg?color=orange)](https://index.scala-lang.org/7mind/sick)
[![License](https://img.shields.io/badge/License-BSD--2--Clause-blue.svg)](LICENSE)

# SICK: Streams of Independent Constant Keys

`SICK` is a representation of `JSON`-like structures.

This repository provides **Efficient Binary Aggregate (EBA)** - a deduplicated binary storage format for JSON based on the `SICK` representation. We provide implementations for Scala, C# and JavaScript.

Sister project: [UEBA](https://github.com/7mind/baboon/blob/main/docs/ueba-format.md), a tagless binary encoding.

## What EBA enables

**Current implementation:**

1. **Store JSON-like data in efficient indexed binary form** - Access nested data without deserializing the entire structure
2. **Avoid reading whole JSON files** - Access only the data you need with lazy loading
3. **Deduplicate storage** - Store multiple JSON-like structures with automatic deduplication of common values

**Future potential:**

The `SICK` representation also enables **efficient streaming** of JSON data - perfect streaming parsers and efficient delta updates. We currently do not
provide streaming abstractions as it's challenging to design a solution that fits all use cases. Contributions are welcome.

## Tradeoffs

Encoding is more complex than traditional JSON serialization, but reading becomes significantly faster and more memory-efficient.

## Implementation Status

| Feature                    | Scala 🟣 | C# 🔵 | JS (ScalaJS) 🟡 |
|---------------------------|----------|-------|-----------------|
| EBA Encoder 💾            | ✅       | ✅    | ✅              |
| EBA Decoder 📥            | ✅       | ✅    | ✅              |
| EBA Encoder AST 🌳        | Circe    | JSON.Net | JS Objects  |
| EBA Decoder AST 🌿        | Circe    | Custom | JS Objects     |
| Cursors 🧭                | ⚠️       | ✅    | ❌              |
| Path Queries 🔍           | ❌       | ✅    | ❌              |
| Stream Encoder 🌊         | ❌       | ❌    | ❌              |
| Stream Decoder 🌀         | ❌       | ❌    | ❌              |

Current Scala API for reading SICK structures is less mature than C# one: only basic abstractions are provided. Contributions are welcome.

## Limitations

Current implementation constraints:

1. **Maximum object size:** 65,534 keys per object
2. **Key order:** Object key order is not preserved (as per [JSON RFC](https://www.rfc-editor.org/rfc/rfc4627#section-1))
3. **Maximum array elements:** 2³² (4,294,967,296) elements
4. **Maximum unique values per type:** 2³² (4,294,967,296) unique values

These limits can be lifted by using more bytes for offsets and counts, though real-world applications rarely approach these limits. Large structures can be split into smaller chunks at the client side.

## Project Status

1. **Battle-tested** - Covered by comprehensive test suites including cross-implementation correctness tests (C# ↔ Scala)
2. **Production-ready** - Powers proprietary applications on mobile devices and browsers, including apps with hundreds of thousands of daily active users
3. **Open source adoption** - No known open source users as of October 2025
4. **Platform support** - Additional platform implementations welcome (Python, Rust, Go, etc.)

## Performance

SICK excels in scenarios with:

- **Large JSON files** - Direct indexed reads are much faster than full JSON parse
- **Repetitive structure** - Deduplication significantly reduces storage
- **Memory constraints** - Incremental reading uses constant memory
- **File size** - usually much more compact than JSON

Tradeoffs:

- **Write overhead** - Encoding is significantly slower than JSON serialization. It can be made faster by partially turning off deduplication.
- **Random access** - Best for selective field access, not full traversal

## A bit of theory and ideas

### The Problem with JSON

`JSON` has a [Type-2](https://en.wikipedia.org/wiki/Chomsky_hierarchy#Type-2_grammars) grammar and requires a [pushdown
automaton](https://en.wikipedia.org/wiki/Pushdown_automaton) to parse it. This makes it impossible to implement an efficient streaming parser for `JSON`.
Consider a deeply nested hierarchy of `JSON` objects: you cannot finish parsing the top-level object until you've processed the entire file.

`JSON` is frequently used to store and transfer large amounts of data, and these transfers tend to [grow over
time](https://nee.lv/2021/02/28/How-I-cut-GTA-Online-loading-times-by-70/). A typical `JSON` config file for a large enterprise product is a good example.

The non-streaming nature of almost all JSON parsers requires substantial work every time you deserialize a large chunk of `JSON` data:
1. Read it from disk
2. Parse it in memory into an AST representation
3. Map the raw `JSON` tree to object instances

Even if you use token streams and know the type of your object ahead of time, you still must deal with the Type-2 grammar.

This can be very inefficient, causing unnecessary delays, pauses, CPU activity spikes, and memory consumption spikes.

### The SICK Solution

SICK transforms hierarchical JSON into a flat, deduplicated table of values with references, enabling:

- **Indexed access** - Jump directly to the data you need
- **Deduplication** - Share common values across multiple structures
- **Streaming capability** - Process data in constant memory
- **Fast queries** - Path-based access without full deserialization

#### Example Transformation

Given this JSON:

```json
[
    {"some key": "some value"},
    {"some key": "some value"},
    {"some value": "some key"}
]
```

SICK creates this flattened table:

| Type   | Index | Value                          | Is Root         |
| ------ | ----- | ------------------------------ | --------------- |
| string | 0     | "some key"                     | No              |
| string | 1     | "some value"                   | No              |
| object | 0     | [string:0, string:1]           | No              |
| object | 1     | [string:1, string:0]           | No              |
| array  | 0     | [object:0, object:0, object:1] | Yes (file.json) |

Notice how duplicate values are stored once and referenced multiple times, and how the structure is completely flat.

#### Streaming

This representation enables many capabilities. For example, we can stream the table:

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

While this particular encoding is inefficient, it's streamable. Moreover, we can add removal messages to support arbitrary updates:

```
array:0[0] = object:1
array:0[1] = remove
```

**Important property:** When a stream does not contain removal entries, it can be safely reordered. This eliminates many cases where full accumulation is required.

Depending on the use case, we can process entries as they arrive and discard them immediately. For example, if we need to sum all fields named `"amount"` across all objects and we have a reference for that name, we can maintain a single accumulator variable and discard everything else as we receive it.

Not all accumulation can be eliminated, though - the receiver may still need to buffer entries until they can be sorted out.

## Quick Start

- [Scala](#scala)
- [C#](#c)
- [JavaScript](#javascript)

### Scala

Add to your `build.sbt`:

```scala
libraryDependencies += "io.7mind.izumi" %% "json-sick" % "<Check for latest version>"
```

**Basic encoding and decoding:**

```scala
//> using scala "2.13"
//> using dep "io.circe::circe-core:0.14.13"
//> using dep "io.circe::circe-jawn:0.14.13"
//> using dep "io.7mind.izumi::json-sick:latest.integration"

import io.circe._
import io.circe.jawn.parse
import izumi.sick.SICK
import izumi.sick.eba.writer.EBAWriter
import izumi.sick.eba.reader.{EagerEBAReader, IncrementalEBAReader}
import izumi.sick.eba.reader.incremental.IncrementalJValue._
import izumi.sick.model.{SICKWriterParameters, TableWriteStrategy}
import izumi.sick.sickcirce.CirceTraverser._
import java.nio.file.{Files, Paths}

object SickExample {
  def main(args: Array[String]): Unit = {
    // Parse JSON string
    val jsonString = """{"name": "Alice", "age": 30, "city": "NYC"}"""
    val json = parse(jsonString).toTry.get

    // Encode to SICK binary format
    val eba = SICK.packJson(
      json = json,
      name = "user.json",
      dedup = true,                // Enable deduplication
      dedupPrimitives = true,      // Deduplicate primitive values too
      avoidBigDecimals = false     // Use BigDecimals for precision
    )

    // Write to bytes
    val (bytes, info) = EBAWriter.writeBytes(
      eba.index,
      SICKWriterParameters(TableWriteStrategy.SinglePassInMemory)
    )

    // Save to file
    val bytesArray = bytes.toArrayUnsafe()
    Files.write(Paths.get("user.sick"), bytesArray)

    // Read back from bytes (eager loading)
    val structure = EagerEBAReader.readEBABytes(bytesArray)

    // Find and reconstruct the root
    val rootEntry = structure.findRoot("user.json").get
    val reconstructed = structure.reconstruct(rootEntry.ref)

    println(reconstructed)  // Back to original JSON

    // Or use incremental reader for efficient field access
    val reader = IncrementalEBAReader.openBytes(bytesArray, eagerOffsets = false)
    try {
      val rootRef = reader.getRoot("user.json").get

      // Read specific fields without full deserialization
      val nameRef = reader.readObjectFieldRef(rootRef, "name")
      val nameValue = reader.resolve(nameRef)
      val name = nameValue match {
        case JString(s) => s
        case _ => throw new IllegalStateException("Expected string")
      }
      println(s"Name: $name")  // "Alice"

      val ageRef = reader.readObjectFieldRef(rootRef, "age")
      val ageValue = reader.resolve(ageRef)
      val age = ageValue match {
        case JByte(b) => b.toInt
        case JShort(s) => s.toInt
        case JInt(i) => i
        case JLong(l) => l.toInt
        case _ => throw new IllegalStateException(s"Expected numeric type, got: $ageValue")
      }
      println(s"Age: $age")  // 30
    } finally {
      reader.close()
    }
  }
}
```

The example above can be saved to a file (e.g., `example.scala`) and run directly with:
```bash
scala-cli example.scala
```

### C#

Install via NuGet:

```bash
dotnet add package SickSharp
```

**Basic encoding and decoding:**

```csharp
#r "nuget: Izumi.SICK, *"
#r "nuget: Newtonsoft.Json, 13.0.3"

using Newtonsoft.Json.Linq;
using SickSharp;
using SickSharp.Encoder;
using SickSharp.Format;
using SickSharp.IO;

// Parse JSON
var jsonString = @"{""name"": ""Alice"", ""age"": 30, ""city"": ""NYC""}";
var json = JToken.Parse(jsonString);

// Create index and append JSON
var index = SickIndex.Create(buckets: 128, limit: 2);
var rootRef = index.Append("user.json", json);

// Serialize to bytes
var serialized = index.Serialize();
File.WriteAllBytes("user.sick", serialized.Data);

// Read back from file
using (var reader = SickReader.OpenFile(
    "user.sick",
    ISickCacheManager.NoCache,
    ISickProfiler.Noop(),
    loadInMemoryThreshold: 32768))
{
    var root = reader.ReadRoot("user.json");
    Console.WriteLine(root);  // Cursor to the root object

    // Read specific fields using cursor API (without full deserialization)
    var nameCursor = root.Read("name");
    var name = nameCursor.AsString();
    Console.WriteLine($"Name: {name}");  // "Alice"

    var ageCursor = root.Read("age");
    var age = ageCursor.AsInt();
    Console.WriteLine($"Age: {age}");  // 30

    // Or convert entire structure back to JSON
    var jsonResult = reader.ToJson(root.Ref);
    Console.WriteLine(jsonResult);
}
```

The example above can be saved to a file (e.g., `example.csx`) and run directly with:
```bash
dotnet script example.csx
```

**Query-based access (C# only):**

```csharp
#r "nuget: Izumi.SICK, *"
#r "nuget: Newtonsoft.Json, 13.0.3"

using SickSharp;
using SickSharp.Format;
using SickSharp.IO;

using (var reader = SickReader.OpenFile("user.sick",
    ISickCacheManager.NoCache,
    ISickProfiler.Noop(),
    loadInMemoryThreshold: 32768))
{
    var root = reader.ReadRoot("user.json");

    // Query using path syntax
    var name = root.Query("name").AsString();
    Console.WriteLine($"Name: {name}");  // "Alice"

    // Query nested structures
    // For {"info": {"version": "1.0.0"}}
    var version = root.Query("info.version").AsString();

    // Query arrays
    // For {"items": ["a", "b", "c"]}
    var firstItem = root.Query("items[0]").AsString();
    var lastItem = root.Query("items[-1]").AsString();
}
```

### JavaScript

Install npm package with

```
npm install @izumi-framework/json-sick
```

See [npm Readme](https://www.npmjs.com/package/@izumi-framework/json-sick?activeTab=readme) for JavaScript API documentation

## Binary format: EBA (Efficient Binary Aggregate)

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

## Binary Format: EBA (Efficient Binary Aggregate)

### Core Concepts

The EBA format uses these fundamental building blocks:

1. **References** - Fixed-size pairs of (type, index) pointing to values in type-specific tables
2. **Type Markers** - Single-byte identifiers for each supported type
3. **Value Tables** - Separate arrays for each type, indexed for O(1) access

### Structure Layout

An EBA file consists of:

```
[Header]
├── Version (4 bytes)
├── Table Offsets Array (4 bytes × table count)
└── Bucket Count (2 bytes)

[Type-Specific Tables]
├── Integers Table
├── Longs Table
├── BigIntegers Table
├── Floats Table
├── Doubles Table
├── BigDecimals Table
├── Strings Table
├── Arrays Table
├── Objects Table
└── Roots Table
```

### References

A reference is a 5-byte structure:

```
[Type Marker: 1 byte][Index: 4 bytes]
```

The type marker identifies which table to look in, and the index identifies the position within that table. This allows instant O(1) lookups without parsing.

**Example:**
- `[10][00 00 00 05]` = String at index 5
- `[11][00 00 00 02]` = Array at index 2
- `[12][00 00 00 00]` = Object at index 0

### Lists

Lists store variable-length sequences efficiently using an offset array:

**Fixed-size elements (e.g., array of references):**
```
[Count: 4 bytes][Elements in sequence]
```

**Variable-size elements (e.g., array of strings):**
```
[Count: 4 bytes][Offset₀: 4 bytes][Offset₁: 4 bytes]...[Offsetₙ: 4 bytes][Data concatenated]
```

For `["a", "bb", "ccc"]`:
```
3 | 0 | 1 | 3 | a | bb | ccc
```

The offset array enables O(1) random access to any element.

### Array Entries

Array entries are simply lists of references:

```
[Count: 4 bytes][Ref₀: 5 bytes][Ref₁: 5 bytes]...[Refₙ: 5 bytes]
```

Each reference points to a value in its respective type table.

### Object Entries

Objects store key-value pairs with an optimization for fast lookups:

```
[Entry Count: 2 bytes][Skip List Data][Key-Value Pairs]
```

**Key-Value Pair:**
```
[Key Index: 4 bytes][Value Reference: 5 bytes]
```

Keys are stored as indices into the strings table, not inline, enabling automatic deduplication of property names across objects.

### Object Skip List and KHash

For fast field lookups, objects use a skip list based on key hashes:

**Skip List Structure:**
```
[Bucket Count: 2 bytes][Bucket₀ Start: 2 bytes][Bucket₁ Start: 2 bytes]...
```

**KHash Algorithm:**
The hash function distributes keys across buckets:

1. Hash the string key using a fast non-cryptographic hash
2. Compute `bucket = hash % bucketCount`
3. Use the skip list to jump to the first entry in that bucket
4. Linear search within the bucket (typically 1-2 entries)

**Example:**

For 128 buckets and keys `["name", "age", "city"]`:

```
Bucket 45: [0]      // "name" at index 0
Bucket 67: [1]      // "age" at index 1
Bucket 89: [2, 65]  // "city" at index 2, end of list at 65
```

This provides near-O(1) lookup while maintaining compact encoding.

### Value Tables

Each type has its own table for storage efficiency:

**Fixed-Size Types (Int, Long, Float, Double):**
```
[Count: 4 bytes][Value₀][Value₁]...[Valueₙ]
```

**Variable-Size Types (String, BigInteger, BigDecimal):**
```
[Count: 4 bytes][Offsets Array][Data concatenated]
```

**Structured Types (Arrays, Objects):**
```
[Count: 4 bytes][Structure₀][Structure₁]...[Structureₙ]
```

**Roots Table:**
```
[Count: 4 bytes][Root₀][Root₁]...[Rootₙ]
```

Each root entry:
```
[Name Index: 4 bytes][Value Reference: 5 bytes]
```

The name index points into the strings table, and the value reference points to the actual root data structure.

## Supported Types

| Marker | Name    | Comment                      | Size (bytes)        | C# Type      | Scala Type |
| ------ | ------- | ---------------------------- | ------------------- | ------------ | ---------- |
| 0      | TNul    | Equivalent to JSON `null`    | 0 (in marker)       | null         | null       |
| 1      | TBit    | Boolean                      | 0 (in marker)       | bool         | Boolean    |
| 2      | TByte   | Unsigned byte                | 0 (in marker)       | byte         | Byte       |
| 3      | TShort  | Signed 16-bit integer        | 0 (in marker)       | short        | Short      |
| 4      | TInt    | Signed 32-bit integer        | 4                   | int          | Int        |
| 5      | TLng    | Signed 64-bit integer        | 8                   | long         | Long       |
| 6      | TBigInt | Arbitrary precision integer  | Variable, prefixed  | BigInteger   | BigInt     |
| 7      | TDbl    | Double-precision float       | 8                   | double       | Double     |
| 8      | TFlt    | Single-precision float       | 4                   | float        | Float      |
| 9      | TBigDec | Arbitrary precision decimal  | Variable, prefixed  | Custom       | BigDecimal |
| 10     | TStr    | UTF-8 String                 | Variable, prefixed  | string       | String     |
| 11     | TArr    | List of array entries        | Variable, prefixed  | Array        | Array      |
| 12     | TObj    | List of object entries       | Variable, prefixed  | Object       | Object     |
| 15     | TRoot   | Root entry (name + ref)      | 9 (4 name + 5 ref)  | Root         | Root       |

### Additional capabilities over `JSON`

`SICK` encoding follows the compositional principles of `JSON` (a set of primitive types plus lists and dictionaries), but is more powerful: it has a
"reference" type and allows encoding custom types.

#### 1. Multiple Roots with Deduplication

We can store multiple JSON files in one table with full deduplication across their content. This is implemented using a separate "root" type, where each
root value contains a reference to its name and a reference to the actual `JSON` value:

| Type   | index | Value                          |
| ------ | ----- | ------------------------------ |
| string | 0     | "some key"                     |
| string | 1     | "some value"                   |
| string | 2     | "some value"                   |
| object | 0     | [string:0, string:1]           |
| object | 1     | [string:1, string:0]           |
| array  | 0     | [object:0, object:0, object:1] |
| root   | 0     | [string:2, array:0]            |

**Status:** ✅ Implemented

#### 2. Circular References

The table representation can store circular references, something `JSON` cannot do natively:

| Type   | index | Value                |
| ------ | ----- | -------------------- |
| object | 0     | [string:0, object:1] |
| object | 1     | [string:1, object:0] |

Here, objects 0 and 1 reference each other. This may be useful in some complex cases.

**Status:** ❌ Not currently supported

#### 3. Custom Scalar Types

The representation can be extended with custom types (e.g., timestamps, UUIDs) by introducing new type markers. This enables native storage of
domain-specific types without string encoding.

**Status:** ❌ Not currently supported

#### 4. Polymorphic Types

The representation can support polymorphic types through custom type tags, enabling efficient storage of variant types.

**Status:** ❌ Not currently supported

## Contributing

Contributions are welcome! Areas of interest:

- Streaming encoder/decoder implementations
- Additional language bindings
- Performance optimizations
- Documentation improvements

