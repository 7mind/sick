# SICK: Streams of Independent Constant Keys

`SICK` is an approach to handle `JSON`-like structures and various libraries implementing it.

`SICK` allows you to achieve the following:

1. Store `JSON`-like data in efficient indexed binary form
2. Avoid reading and parsing whole `JSON` files and access the data you need just in time
3. Store multiple `JSON`-like structures in one deduplicating storage
4. Implement ideal streaming parsers for `JSON`-like data
5. Efficiently stream updates for `JSON`-like data

The tradeoff for these benefits is somehow more complicated and less efficient encoder.

## The problem

`JSON` has a [Type-2](https://en.wikipedia.org/wiki/Chomsky_hierarchy#Type-2_grammars) grammar and requires a pushdown automaton to parse it. So, it's not possible to implement efficient streaming parser for `JSON`. Just imagine a huge hierarchy of nested `JSON` objects: you won't be able to finish parsing the top-level object until the whole file.

`JSON` is frequently used to store and transfer large amounts of data and these transfers tend to grow over time. Just imagine a typical `JSON` config file for a large enterprise product.

The non-streaming nature of almost all the JSON parsers requires a lot of work to be done every time you need to deserialize your a huge chunk of `JSON` data: you need to read it from disk, parse it and, usually, map raw `JSON` tree to object instances.

This may be very inefficient and cause unnecessary delays and pauses.

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
| object | 0     | [string:0, string,1]           | No              |
| object | 1     | [string:1, string:0]           | No              |
| array  | 0     | [object:0, object:0, object:1] | Yes (file.json) |

This way we flattened and deduplicated our `JSON`.

### Streaming

Now we may do manu different things, for example we may stream our table:

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

root:0=array.0,string:2
```

This particular encoding is inefficient but it's streamable and, moreover, we can extend with removal message to support arbitrary updates:

```
array:0[0] = object:1
array:0[1] = remove
```

There is an interesting observation: the initial stream entries (when there is no removals) may be safely reordered, though sometimes the receiver would need to store them until it can sort them out.

### Binary storage

We may note that the only complex data structures in our "Value" column are lists and `(type, index)` pairs. Let's call the pairs "references".

A reference can be represented as a pair of integers, so it would have a fixed byte length.

A list of references can be represented as an integer storing list length followed by all the references' bytes. Let's note that such binary structure is indexed, when we know the index of the element we want to access we can do it immediately.

A list of any fixed-size scalar values can be represented the same way.

A list of variable-size values (e.g. a list of strings) can be represented the following way:

```
  {strings count}{list of string offsets}{all the strings concatenated}
```

So, `["a", "bb", "ccc"]` would become something like `3 0 2 3 a b bb ccc` without spaces.

An important fact is that this encoding is indexed too and it can be reused to store any lists of variable-length data.


## Implementation

TODO
### Additional capabilities over `JSON`

TODO: multiple roots, circular references

### Streaming

TODO
### Efficient binary indexed storage

TODO
