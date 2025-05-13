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

            Ints = new IntTable(_stream, Header.Offsets[0]);
            Longs = new LongTable(_stream, Header.Offsets[1]);
            BigInts = new BigIntTable(_stream, Header.Offsets[2]);

            Floats = new FloatTable(_stream, Header.Offsets[3]);
            Doubles = new DoubleTable(_stream, Header.Offsets[4]);
            BigDecimals = new BigDecTable(_stream, Header.Offsets[5]);

            Strings = new StringTable(_stream, Header.Offsets[6]);

            Arrs = new ArrTable(_stream, Header.Offsets[7]);
            Objs = new ObjTable(_stream, Strings, Header.Offsets[8], Header.Settings);
            Roots = new RootTable(_stream, Header.Offsets[9]);

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
                var buf = ReadAllBytes2(path);
                stream = new MemoryStream(buf, 0, buf.Length, writable: false, publiclyVisible: true);
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
                if (fileLength > int.MaxValue) throw new IOException($"{filePath} is too large");

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

#if SICK_DEBUG_TRAVEL
        public static volatile int TotalLookups = 0;
        public static volatile int TotalTravel = 0;
#endif

        public Ref? GetRoot(string id)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("GetRoot", id))
#endif
            {
                Ref? value;
#if SICK_PROFILE_READER
                return cp.OnReturn(_roots.TryGetValue(id, out value) ? value : null);
#else
                return _roots.TryGetValue(id, out value) ? value : null;
#endif
            }
        }

        public Ref ReadObjectFieldRef(Ref reference, string field)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("ReadObjectFieldRef", reference, field))
#endif
            {
                if (reference.Kind == RefKind.Obj)
                {
                    var currentObj = Objs.Read(reference.Value);

#if SICK_PROFILE_READER
                    return cp.OnReturn(ReadObjectFieldRef(field, currentObj, $"lookup in ReadObjectFieldRef starting with `{reference}`"));
#else
                    return ReadObjectFieldRef(field, currentObj, new Lazy<string>(() => $"lookup in ReadObjectFieldRef starting with `{reference}`"));
#endif
                }

                throw new KeyNotFoundException(
                    $"Tried to find field `{field}` in entity with id `{reference}` which should be an object, but it was `{reference.Kind}`"
                );
            }
        }

        private Ref ReadObjectFieldRef(string field, OneObjTable currentObj, Lazy<string> clue)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("ReadObjectFieldRef", field, currentObj, clue.Value))
#endif
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
                    var probablyLower = currentObj.BucketValue(bucket);

                    if (probablyLower == ObjIndexing.MaxIndex)
                    {
                        throw new KeyNotFoundException(
                            $"Field `{field}` not found in object `{currentObj}`. Context: {clue.Value}"
                        );
                    }

                    if (probablyLower >= currentObj.Count)
                    {
                        throw new FormatException(
                            $"Structural failure: Field `{field}` in object `{currentObj}` produced bucket index `{probablyLower}` which is more than object size `{currentObj.Count}`. Context: {clue.Value}"
                        );
                    }

                    lower = probablyLower;

                    // with optimized index there should be no maxIndex elements in the index and we expect to make exactly ONE iteration
                    for (uint i = bucket + 1; i < Header.Settings.BucketCount; i++)
                    {
                        var probablyUpper = currentObj.BucketValue(i);

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
                                $"Field `{field}` in object `{currentObj}` produced bucket index `{probablyUpper}` which is more than object size `{currentObj.Count}`. Context: {clue.Value}"
                            );
                        }
                    }
                }


#if SICK_DEBUG_TRAVEL
                TotalLookups += 1;
#endif

                Debug.Assert(lower <= upper);
                for (int i = lower; i < upper; i++)
                {
                    var bytes = currentObj.ReadKeyOnly(i, out var key);
                    if (key == field)
                    {
#if SICK_DEBUG_TRAVEL
                        TotalTravel += (i - lower);
#endif

                        var kind = (RefKind)bytes[sizeof(int)];
                        var value = bytes[(sizeof(int) + 1)..(sizeof(int) * 2 + 1)].ReadInt32BE();
#if SICK_PROFILE_READER
                        return cp.OnReturn(new Ref(kind, value));
#else
                        return new Ref(kind, value);
#endif
                    }
                }

                throw new KeyNotFoundException(
                    $"Field `{field}` not found in object `{currentObj}`. Context: {clue.Value}"
                );
            }
        }



        public Ref ReadArrayElementRef(Ref reference, int iindex)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("ReadArrayElementRef", reference, iindex))
#endif
            {
                if (reference.Kind == RefKind.Arr)
                {
                    var currentObj = Arrs.Read(reference.Value);
                    var i = (iindex >= 0)
                        ? iindex
                        : currentObj.Count + iindex; // + decrements here because iindex is negative
                    var ret = currentObj.Read(i);
#if SICK_PROFILE_READER
                    return cp.OnReturn(ret);
#else
                    return ret;
#endif
                }

                throw new KeyNotFoundException(
                    $"Tried to find element `{iindex}` in entity with id `{reference}` which should be an array, but it was `{reference.Kind}`"
                );
            }
        }


        public JToken ToJson(Ref reference)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("ToJson", reference))
#endif
            {
                JToken ret;
                switch (reference.Kind)
                {
                    case RefKind.Nul:
                        ret = JValue.CreateNull();
                        break;
                    case RefKind.Bit:
                        ret = new JValue(reference.Value == 1);
                        break;
                    case RefKind.SByte:
                        ret = new JValue((sbyte)reference.Value);
                        break;

                    case RefKind.Short:
                        ret = new JValue((short)reference.Value);
                        break;
                    case RefKind.Int:
                        ret = new JValue(Ints.Read(reference.Value));
                        break;
                    case RefKind.Lng:
                        ret = new JValue(Longs.Read(reference.Value));
                        break;
                    case RefKind.BigInt:
                        ret = new JValue(BigInts.Read(reference.Value));
                        break;
                    case RefKind.Flt:
                        ret = new JValue(Floats.Read(reference.Value));
                        break;
                    case RefKind.Dbl:
                        ret = new JValue(Doubles.Read(reference.Value));
                        break;
                    case RefKind.BigDec:
                        ret = new JValue(BigDecimals.Read(reference.Value));
                        break;
                    case RefKind.Str:
                        ret = new JValue(Strings.Read(reference.Value));
                        break;
                    case RefKind.Arr:
                        ret = new JArray(
                            new SingleShotEnumerable<Ref>(Arrs.Read(reference.Value).GetEnumerator())
                                .Select(ToJson).ToArray<object>());
                        break;
                    case RefKind.Obj:
                        ret = new JObject(
                            new SingleShotEnumerable<KeyValuePair<string, Ref>>(Objs.Read(reference.Value)
                                    .GetEnumerator())
                                .Select(kvp => new JProperty(kvp.Key, ToJson(kvp.Value))).ToArray<object>());
                        break;
                    case RefKind.Root:
                        ret = ToJson(Roots.Read(reference.Value).Reference);
                        break;
                    default:
                        throw new InvalidDataException($"BUG: Unknown reference: `{reference}`");
                }

#if SICK_PROFILE_READER
                    return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }

        public IJsonVal Resolve(Ref reference)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("Resolve", reference))
#endif
            {
                IJsonVal ret;
                switch (reference.Kind)
                {
                    case RefKind.Nul:
                        ret = new JNull();
                        break;
                    case RefKind.Bit:
                        ret = new JBool(reference.Value == 1);
                        break;
                    case RefKind.SByte:
                        ret = new JSByte((sbyte)reference.Value);
                        break;
                    case RefKind.Short:
                        ret = new JShort((short)reference.Value);
                        break;
                    case RefKind.Int:
                        ret = new JInt(Ints.Read(reference.Value));
                        break;
                    case RefKind.Lng:
                        ret = new JLong(Longs.Read(reference.Value));
                        break;
                    case RefKind.BigInt:
                        ret = new JBigInt(BigInts.Read(reference.Value));
                        break;
                    case RefKind.Flt:
                        ret = new JSingle(Floats.Read(reference.Value));
                        break;
                    case RefKind.Dbl:
                        ret = new JDouble(Doubles.Read(reference.Value));
                        break;
                    case RefKind.BigDec:
                        ret = new JBigDecimal(BigDecimals.Read(reference.Value));
                        break;
                    case RefKind.Str:
                        ret = new JStr(Strings.Read(reference.Value));
                        break;
                    case RefKind.Arr:
                        ret = new JArr(Arrs.Read(reference.Value));
                        break;
                    case RefKind.Obj:
                        ret = new JObj(Objs.Read(reference.Value));
                        break;
                    case RefKind.Root:
                        ret = new JRoot(Roots.Read(reference.Value));
                        break;
                    default:
                        throw new InvalidDataException($"BUG: Unknown reference: `{reference}`");
                }

#if SICK_PROFILE_READER
                    return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }

        public void Dispose()
        {
            _stream.Close();
        }

        private Header ReadHeader()
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("ReadHeader"))
#endif
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
                        $"Structural failure: SICK table count expected to be {expectedTableCount}, got {tableCount}, stream {_stream}");
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
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("Query", path))
#endif
            {
                var ret = Query(reference, path.Split('.'));

#if SICK_PROFILE_READER
                return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }

        public FoundRef QueryRef(Ref reference, string path)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("QueryRef", path))
#endif
            {
                var query = path.Split('.');
                var ret = new FoundRef(QueryRef(reference, query), query);

#if SICK_PROFILE_READER
                return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }


        public IJsonVal Query(Ref reference, Span<string> path)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("Query/span", reference, new Lazy<string[]>(path.ToArray())))
#endif
            {
                var result = QueryRef(reference, path);
                var value = Resolve(result);
                if (value == null)
                {
                    throw new KeyNotFoundException(
                        $"Failed to query `{reference}` lookup result was `{result}` but it failed to resolve. The query was `{String.Join("->", path.ToArray())}`"
                    );
                }

#if SICK_PROFILE_READER
                return cp.OnReturn(value);
#else
                return value;
#endif
            }
        }

        public Ref QueryRef(Ref reference, Span<string> path)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("QueryRef/span", reference, new Lazy<string[]>(path.ToArray())))
#endif
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

                if (next.Length == 0)
                {
                    return resolvedObj;
                }

                return QueryRef(resolvedObj, next);
            }
        }

        public IJsonVal Query(JObj jObj, string path)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("Query", jObj, path))
#endif
            {
                var ret = QueryJsonVal(jObj, path.Split('.'), jObj, path);
#if SICK_PROFILE_READER
                return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }


        private IJsonVal QueryJsonVal(JObj jObj, Span<string> path, JObj initialObj, string initialQuery)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("QueryJsonVal", jObj, initialObj, initialQuery, new Lazy<string[]>(path.ToArray())))
#endif
            {
                if (path.Length == 0)
                {
                    return jObj;
                }

                var currentQuery = path[0];
                var next = HandleBracketsWithoutDot(ref currentQuery, path);

                var resolvedObj = ReadObjectFieldRef(currentQuery, jObj.Value, new Lazy<string>(() => $"query `{initialQuery}` on object `{initialObj}`"));

                if (next.Length == 0)
                {
                    return Resolve(resolvedObj);
                }

                var ret = Query(resolvedObj, next);

#if SICK_PROFILE_READER
                return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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