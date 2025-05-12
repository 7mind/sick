#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SickSharp.Format.Tables;
using SickSharp.Primitives;

namespace SickSharp.Format
{
    public record FoundRef(Ref result, string[] query);

    /// <summary>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>This class is thread unsafe as per it encapsulates a seekable stream!</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Instances produced by this class are only valid while the instance of the class itself is not
    ///                 finalized!
    ///             </description>
    ///         </item>
    ///     </list>
    /// </summary>
    /// <threadsafety instance="false" />
    public class SickReader : IDisposable
    {
        private readonly Dictionary<string, Ref> _roots = new();
        private readonly Stream _stream;
        private readonly ISickProfiler _profiler;

        public static bool LoadIndexes { get; set; }


        static SickReader()
        {
            LoadIndexes = true;

            if (!BitConverter.IsLittleEndian)
            {
                throw new ConstraintException("big endian architecure is not supported");
            }
        }

        public SickReader(Stream stream, ISickProfiler profiler)
        {
            _stream = stream;
            _profiler = profiler;
            Header = ReadHeader();

            Ints = new IntTable(_stream, (UInt32)Header.Offsets[0]);
            Longs = new LongTable(_stream, (UInt32)Header.Offsets[1]);
            BigInts = new BigIntTable(_stream, (UInt32)Header.Offsets[2]);

            Floats = new FloatTable(_stream, (UInt32)Header.Offsets[3]);
            Doubles = new DoubleTable(_stream, (UInt32)Header.Offsets[4]);
            BigDecimals = new BigDecTable(_stream, (UInt32)Header.Offsets[5]);

            Strings = new StringTable(_stream, (UInt32)Header.Offsets[6]);

            Arrs = new ArrTable(_stream, (UInt32)Header.Offsets[7]);
            Objs = new ObjTable(_stream, Strings, (UInt32)Header.Offsets[8], Header.Settings);
            Roots = new RootTable(_stream, (UInt32)Header.Offsets[9]);

            for (var i = 0; i < Roots.Count; i++)
            {
                var rootEntry = Roots.Read(i);
                var rootId = Strings.Read(rootEntry.Key);
                var root = rootEntry.Reference;

                _roots.Add(rootId, root);
            }
        }

        public static SickReader OpenFile(
            string path,
            ISickCacheManager cacheManager,
            ISickProfiler profiler,
            long inMemoryThreshold = 65536,
            bool pageCached = true,
            int cachePageSize = 4192
        )
        {
            var info = new FileInfo(path);
            var loadIntoMemory = info.Length <= inMemoryThreshold;

            Stream stream;
            if (loadIntoMemory)
            {
                // stream = new MemoryStream(File.ReadAllBytes(path));
                // there were reports that ReadAllBytes might be broken on IL2CPP
                stream = new MemoryStream(ReadAllBytes2(path));
                
            }
            else if (pageCached)
            {
                stream = new NonAllocPageCachedStream(cacheManager.Provide(path, cachePageSize, profiler));
            }
            else
            {
                stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            return new SickReader(stream, profiler);
        }
        
        private static byte[] ReadAllBytes2(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long fileLength = fs.Length;
                if (fileLength > int.MaxValue)
                    throw new IOException($"{filePath} is too large");

                byte[] bytes = new byte[fileLength];

                fs.ReadUpTo(bytes, 0, (int)fileLength);
                
                return bytes;
            }
        }



        public Header Header { get; }
        public IntTable Ints { get; }
        public LongTable Longs { get; }
        public BigIntTable BigInts { get; }
        public FloatTable Floats { get; }
        public DoubleTable Doubles { get; }
        public BigDecTable BigDecimals { get; }
        public StringTable Strings { get; }
        public ArrTable Arrs { get; }
        public ObjTable Objs { get; }
        public RootTable Roots { get; }

#if DEBUG_TRAVEL
        public static volatile int TotalLookups = 0;
        public static volatile int TotalTravel = 0;
#endif

        public Ref? GetRoot(string id)
        {
            using (var cp = _profiler.OnInvoke("GetRoot", id))
            {
                Ref? value;
                return cp.OnReturn(_roots.TryGetValue(id, out value) ? value : null);
            }
        }

        public Ref ReadObjectFieldRef(Ref reference, string field)
        {
            using (var cp = _profiler.OnInvoke("ReadObjectFieldRef", reference, field))
            {
                if (reference.Kind == RefKind.Obj)
                {
                    var currentObj = Objs.Read(reference.Value);

                    return cp.OnReturn(ReadObjectFieldRef(field, currentObj,
                        $"lookup in ReadObjectFieldRef starting with `{reference}`"));
                }

                throw new KeyNotFoundException(
                    $"Tried to find field `{field}` in entity with id `{reference}` which should be an object, but it was `{reference.Kind}`"
                );
            }
        }

        private Ref ReadObjectFieldRef(string field, OneObjTable currentObj, String clue)
        {
            using (var cp = _profiler.OnInvoke("ReadObjectFieldRef", field, currentObj, clue))
            {
                var lower = 0;
                var upper = currentObj.Count;

                if (currentObj.UseIndex)
                {
                    //BucketStartOffsets = new ushort[settings.BucketCount];
                    //BucketEndOffsets = new Dictionary<uint, ushort>();

                    // uint previousBucketStart = 0;

                    var khash = KHash.Compute(field);
                    var bucket = Convert.ToUInt32(khash / Header.Settings.BucketSize);
                    var probablyLower = BucketValue(currentObj, bucket);

                    if (probablyLower == ObjIndexing.MaxIndex)
                    {
                        throw new KeyNotFoundException(
                            $"Field `{field}` not found in object `{currentObj}`. Context: {clue}"
                        );
                    }

                    if (probablyLower >= currentObj.Count)
                    {
                        throw new FormatException(
                            $"Structural failure: Field `{field}` in object `{currentObj}` produced bucket index `{probablyLower}` which is more than object size `{currentObj.Count}`. Context: {clue}"
                        );
                    }

                    lower = probablyLower;

                    // with optimized index there should be no maxIndex elements in the index and we expect to make exactly ONE iteration
                    for (uint i = bucket + 1; i < Header.Settings.BucketCount; i++)
                    {
                        var probablyUpper = BucketValue(currentObj, i);

                        if (probablyUpper <= currentObj.Count)
                        {
                            upper = probablyUpper;
                            break;
                        }

                        if (probablyUpper == ObjIndexing.MaxIndex)
                        {
                            continue;
                        }

                        if (probablyUpper > currentObj.Count)
                        {
                            throw new FormatException(
                                $"Field `{field}` in object `{currentObj}` produced bucket index `{probablyUpper}` which is more than object size `{currentObj.Count}`. Context: {clue}"
                            );
                        }
                    }
                }


#if DEBUG_TRAVEL
                TotalLookups += 1;
#endif

                Debug.Assert(lower <= upper);
                for (int i = lower; i < upper; i++)
                {
                    var k = currentObj.ReadKeyOnly(i);
                    if (k.Key == field)
                    {
#if DEBUG_TRAVEL
                        TotalTravel += (i - lower);
#endif

                        var kind = (RefKind)k.Value[sizeof(int)];
                        var value = k.Value[(sizeof(int) + 1)..(sizeof(int) * 2 + 1)].ReadInt32BE();
                        return cp.OnReturn(new Ref(kind, value));
                    }
                }

                throw new KeyNotFoundException(
                    $"Field `{field}` not found in object `{currentObj}`. Context: {clue}"
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort BucketValue(OneObjTable table, UInt32 bucket)
        {
            return table.RawIndex.ReadUInt16BE(ObjIndexing.IndexMemberSize * bucket);
        }

        public Ref ReadArrayElementRef(Ref reference, int iindex)
        {
            using (var cp = _profiler.OnInvoke("ReadArrayElementRef", reference, iindex))
            {
                if (reference.Kind == RefKind.Arr)
                {
                    var currentObj = Arrs.Read(reference.Value);
                    var i = (iindex >= 0)
                        ? iindex
                        : currentObj.Count + iindex; // + decrements here because iindex is negative
                    return cp.OnReturn(currentObj.Read(i));
                }

                throw new KeyNotFoundException(
                    $"Tried to find element `{iindex}` in entity with id `{reference}` which should be an array, but it was `{reference.Kind}`"
                );
            }
        }


        public JToken ToJson(Ref reference)
        {
            using (var cp = _profiler.OnInvoke("ToJson", reference))
            {
                switch (reference.Kind)
                {
                    case RefKind.Nul:
                        return cp.OnReturn(JValue.CreateNull());
                    case RefKind.Bit:
                        return cp.OnReturn(new JValue(reference.Value == 1));
                    case RefKind.SByte:
                        return cp.OnReturn(new JValue((sbyte)reference.Value));
                    case RefKind.Short:
                        return new JValue((short)reference.Value);
                    case RefKind.Int:
                        return cp.OnReturn(new JValue(Ints.Read(reference.Value)));
                    case RefKind.Lng:
                        return cp.OnReturn(new JValue(Longs.Read(reference.Value)));
                    case RefKind.BigInt:
                        return cp.OnReturn(new JValue(BigInts.Read(reference.Value)));
                    case RefKind.Flt:
                        return cp.OnReturn(new JValue(Floats.Read(reference.Value)));
                    case RefKind.Dbl:
                        return cp.OnReturn(new JValue(Doubles.Read(reference.Value)));
                    case RefKind.BigDec:
                        return cp.OnReturn(new JValue(BigDecimals.Read(reference.Value)));
                    case RefKind.Str:
                        return cp.OnReturn(new JValue(Strings.Read(reference.Value)));
                    case RefKind.Arr:
                        return cp.OnReturn(new JArray(
                            new SingleShotEnumerable<Ref>(Arrs.Read(reference.Value).GetEnumerator())
                                .Select(ToJson).ToArray<object>()));
                    case RefKind.Obj:
                        return cp.OnReturn(new JObject(
                            new SingleShotEnumerable<KeyValuePair<string, Ref>>(Objs.Read(reference.Value)
                                    .GetEnumerator())
                                .Select(kvp => new JProperty(kvp.Key, ToJson(kvp.Value))).ToArray<object>()));
                    case RefKind.Root:
                        return cp.OnReturn(ToJson(Roots.Read(reference.Value).Reference));
                    default:
                        throw new InvalidDataException($"BUG: Unknown reference: `{reference}`");
                }
            }
        }

        public IJsonVal Resolve(Ref reference)
        {
            using (var cp = _profiler.OnInvoke("Resolve", reference))
            {
                switch (reference.Kind)
                {
                    case RefKind.Nul:
                        return cp.OnReturn(new JNull());
                    case RefKind.Bit:
                        return cp.OnReturn(new JBool(reference.Value == 1));
                    case RefKind.SByte:
                        return cp.OnReturn(new JSByte((sbyte)reference.Value));
                    case RefKind.Short:
                        return cp.OnReturn(new JShort((short)reference.Value));
                    case RefKind.Int:
                        return cp.OnReturn(new JInt(Ints.Read(reference.Value)));
                    case RefKind.Lng:
                        return cp.OnReturn(new JLong(Longs.Read(reference.Value)));
                    case RefKind.BigInt:
                        return cp.OnReturn(new JBigInt(BigInts.Read(reference.Value)));
                    case RefKind.Flt:
                        return cp.OnReturn(new JSingle(Floats.Read(reference.Value)));
                    case RefKind.Dbl:
                        return cp.OnReturn(new JDouble(Doubles.Read(reference.Value)));
                    case RefKind.BigDec:
                        return cp.OnReturn(new JBigDecimal(BigDecimals.Read(reference.Value)));
                    case RefKind.Str:
                        return cp.OnReturn(new JStr(Strings.Read(reference.Value)));
                    case RefKind.Arr:
                        return cp.OnReturn(new JArr(Arrs.Read(reference.Value)));
                    case RefKind.Obj:
                        return cp.OnReturn(new JObj(Objs.Read(reference.Value)));
                    case RefKind.Root:
                        return cp.OnReturn(new JRoot(Roots.Read(reference.Value)));
                    default:
                        throw new InvalidDataException($"BUG: Unknown reference: `{reference}`");
                }
            }
        }

        public void Dispose()
        {
            _stream.Close();
        }

        private Header ReadHeader()
        {
            using (var cp = _profiler.OnInvoke("ReadHeader"))
            {
                _stream.Position = 0;

                var version = _stream.ReadInt32BE();
                var tableCount = _stream.ReadInt32BE();

                var expectedVersion = 0;
                if (version != expectedVersion)
                {
                    throw new FormatException(
                        $"Structural failure: SICK version expected to be {expectedVersion}, got {version}");
                }

                var expectedTableCount = 10;
                if (tableCount != expectedTableCount)
                {
                    throw new FormatException(
                        $"Structural failure: SICK table count expected to be {expectedTableCount}, got {tableCount}");
                }

                var tableOffsets = new List<int>();

                foreach (var t in Enumerable.Range(0, tableCount))
                {
                    var next = _stream.ReadInt32BE();
                    if (t > 0 && next <= tableOffsets[t - 1])
                    {
                        throw new FormatException(
                            $"Structural failure: wrong SICK format, table offset {next} expected to be more than previous table offset {tableOffsets[t - 1]}");
                    }

                    tableOffsets.Add(next);
                }

                var bucketCount = _stream.ReadUInt16BE();

                // Console.WriteLine($"Offsets: {String.Join(",", tables)}, b SICKuckets: {bucketCount}" );
                var header = new Header(version, tableCount, tableOffsets, new ObjIndexing(bucketCount, 0));
                return header;
            }
        }

        public bool TryQuery(JObj @ref, string fullPath, out IJsonVal o)
        {
            try
            {
                o = Query(@ref, fullPath);
                return true;
            }
            catch
            {
                o = default!;
                return false;
            }
        }

        public bool TryQuery(Ref @ref, string fullPath, out IJsonVal o)
        {
            try
            {
                o = Query(@ref, fullPath);
                return true;
            }
            catch
            {
                o = default!;
                return false;
            }
        }

        public IJsonVal Query(Ref reference, string path)
        {
            using (var cp = _profiler.OnInvoke("Query", path))
            {
                return cp.OnReturn(Query(reference, path.Split('.')));
            }
        }

        public FoundRef QueryRef(Ref reference, string path)
        {
            using (var cp = _profiler.OnInvoke("QueryRef", path))
            {
                var query = path.Split('.');
                return cp.OnReturn(new FoundRef(QueryRef(reference, query), query));
            }
        }


        public IJsonVal Query(Ref reference, Span<string> path)
        {
            using (var cp = _profiler.OnInvoke("Query/span", reference))
            {
                var result = QueryRef(reference, path);
                var value = Resolve(result);
                if (value == null)
                {
                    throw new KeyNotFoundException(
                        $"Failed to query `{reference}` lookup result was `{result}` but it failed to resolve. The query was `{String.Join("->", path.ToArray())}`"
                    );
                }

                return cp.OnReturn(value);
            }
        }

        public Ref QueryRef(Ref reference, Span<string> path)
        {
            using (var cp = _profiler.OnInvoke("QueryRef/span", reference))
            {
                if (path.Length == 0)
                {
                    return reference;
                }

                var currentQuery = path[0];
                var next = HandleBracketsWithoutDot(ref currentQuery, path);

                if (currentQuery.StartsWith("[") && currentQuery.EndsWith("]"))
                {
                    var index = currentQuery.Substring(1, currentQuery.Length - 2);
                    var iindex = Int32.Parse(index);

                    var resolvedArr = ReadArrayElementRef(reference, iindex);
                    return QueryRef(resolvedArr, next);
                }

                var resolvedObj = ReadObjectFieldRef(reference, currentQuery);

                return QueryRef(resolvedObj, next);
            }
        }

        public IJsonVal Query(JObj jObj, string path)
        {
            using (var cp = _profiler.OnInvoke("Query", jObj, path))
            {
                return cp.OnReturn(QueryJsonVal(jObj, path.Split('.'), jObj, path));
            }
        }


        private IJsonVal QueryJsonVal(JObj jObj, Span<string> path, JObj initialObj, string initialQuery)
        {
            using (var cp = _profiler.OnInvoke("QueryJsonVal", jObj, initialObj, initialQuery))
            {
                if (path.Length == 0)
                {
                    return jObj;
                }

                var currentQuery = path[0];
                var next = HandleBracketsWithoutDot(ref currentQuery, path);

                var resolvedObj = ReadObjectFieldRef(currentQuery, jObj.Value,
                    $"query `{initialQuery}` on object `{initialObj}`");

                return cp.OnReturn(Query(resolvedObj, next));
            }
        }

        private static Span<string> HandleBracketsWithoutDot(ref string currentQuery, Span<string> current)
        {
            var span = current.Slice(1);

            var maybeIndex = ExtractIndex(ref currentQuery);
            if (maybeIndex != null)
            {
                var newArray = new string[span.Length + 1];
                newArray[0] = maybeIndex;
                span.CopyTo(newArray.AsSpan(1));
                span = newArray;
            }

            return span;
        }

        private static string? ExtractIndex(ref string currentQuery)
        {
            var indexStart = currentQuery.IndexOf('[');
            // we have [ but not as the first symbol
            if (indexStart > 0 && currentQuery.EndsWith(']'))
            {
                var index = currentQuery.Substring(indexStart);
                currentQuery = currentQuery.Substring(0, indexStart);
                return index;
            }

            return null;
        }
    }
}