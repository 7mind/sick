#nullable enable
using System;
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
    public record FoundRef(Ref result, List<String> query);

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

        public static bool LoadIndexes { get; set; }

        static SickReader()
        {
            LoadIndexes = true;

            if (!BitConverter.IsLittleEndian)
            {
                throw new ConstraintException("big endian architecure is not supported");
            }
        }

        public static SickReader OpenFile(string path, long inMemoryThreshold = 65536, bool pageCached = true, bool nonAllocPageCache = true, int cachePageSize = 4192)
        {
            var info = new FileInfo(path);
            var loadIntoMemory = info.Length <= inMemoryThreshold;
            
            Stream stream;
            if (loadIntoMemory)
            {
                stream = new MemoryStream(File.ReadAllBytes(path));
            }
            else if (pageCached)
            {
                if (nonAllocPageCache)
                {
                    stream = new NonAllocPageCachedStream(path, cachePageSize);
                }
                else
                {
                    stream = new PageCachedStream(path, cachePageSize);
                }
                return new SickReader(stream);
            }
            else
            {
                stream = File.Open(path, FileMode.Open);
            }

            return new SickReader(stream);
        }

        public SickReader(Stream stream)
        {
            _stream = stream;
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
            Ref? value;
            return _roots.TryGetValue(id, out value) ? value : null;
        }

        public IJsonVal Query(Ref reference, string path)
        {
            return Query(reference, path.Split('.').ToList());
        }

        public FoundRef QueryRef(Ref reference, string path)
        {
            var query = path.Split('.').ToList();
            return new FoundRef(QueryRef(reference, query), query);
        }

        public Ref ReadObjectFieldRef(Ref reference, string field)
        {
            if (reference.Kind == RefKind.Obj)
            {
                var currentObj = Objs.Read(reference.Value);

                return ReadObjectFieldRef(field, currentObj,
                    $"lookup in ReadObjectFieldRef starting with `{reference}`");
            }

            throw new KeyNotFoundException(
                $"Tried to find field `{field}` in entity with id `{reference}` which should be an object, but it was `{reference.Kind}`"
            );
        }

        private Ref ReadObjectFieldRef(string field, OneObjTable currentObj, String clue)
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
                    return new Ref(kind, value);
                }
            }

            throw new KeyNotFoundException(
                $"Field `{field}` not found in object `{currentObj}`. Context: {clue}"
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort BucketValue(OneObjTable table, UInt32 bucket)
        {
            return table.RawIndex.ReadUInt16BE(ObjIndexing.IndexMemberSize * bucket);
        }

        public Ref ReadArrayElementRef(Ref reference, int iindex)
        {
            if (reference.Kind == RefKind.Arr)
            {
                var currentObj = Arrs.Read(reference.Value);
                var i = (iindex >= 0)
                    ? iindex
                    : currentObj.Count + iindex; // + decrements here because iindex is negative
                return currentObj.Read(i);
            }

            throw new KeyNotFoundException(
                $"Tried to find element `{iindex}` in entity with id `{reference}` which should be an array, but it was `{reference.Kind}`"
            );
        }


        private Ref QueryRef(Ref reference, List<string> parts)
        {
            if (parts.Count == 0)
            {
                return reference;
            }

            var currentQuery = parts.First();
            var next = parts.Skip(1).ToList();

            HandleBracketsWithoutDot(ref currentQuery, next);

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

        private IJsonVal Query(Ref reference, List<string> parts)
        {
            var result = QueryRef(reference, parts);
            var value = Resolve(result);
            if (value == null)
            {
                throw new KeyNotFoundException(
                    $"Failed to query `{reference}` lookup result was `{result}` but it failed to resolve. The query was `{String.Join("->", parts)}`"
                );
            }

            return value;
        }

        public JToken ToJson(Ref reference)
        {
            switch (reference.Kind)
            {
                case RefKind.Nul:
                    return JValue.CreateNull();
                case RefKind.Bit:
                    return new JValue(reference.Value == 1);
                case RefKind.SByte:
                    return new JValue((sbyte)reference.Value);
                case RefKind.Short:
                    return new JValue((short)reference.Value);
                case RefKind.Int:
                    return new JValue(Ints.Read(reference.Value));
                case RefKind.Lng:
                    return new JValue(Longs.Read(reference.Value));
                case RefKind.BigInt:
                    return new JValue(BigInts.Read(reference.Value));
                case RefKind.Flt:
                    return new JValue(Floats.Read(reference.Value));
                case RefKind.Dbl:
                    return new JValue(Doubles.Read(reference.Value));
                case RefKind.BigDec:
                    return new JValue(BigDecimals.Read(reference.Value));
                case RefKind.Str:
                    return new JValue(Strings.Read(reference.Value));
                case RefKind.Arr:
                    return new JArray(new SingleShotEnumerable<Ref>(Arrs.Read(reference.Value).GetEnumerator())
                        .Select(ToJson).ToArray<object>());
                case RefKind.Obj:
                    return new JObject(
                        new SingleShotEnumerable<KeyValuePair<string, Ref>>(Objs.Read(reference.Value).GetEnumerator())
                            .Select(kvp => new JProperty(kvp.Key, ToJson(kvp.Value))).ToArray<object>());
                case RefKind.Root:
                    return ToJson(Roots.Read(reference.Value).Reference);
                default:
                    throw new InvalidDataException($"BUG: Unknown reference: `{reference}`");
            }
        }

        public IJsonVal Resolve(Ref reference)
        {
            switch (reference.Kind)
            {
                case RefKind.Nul:
                    return new JNull();
                case RefKind.Bit:
                    return new JBool(reference.Value == 1);
                case RefKind.SByte:
                    return new JSByte((sbyte)reference.Value);
                case RefKind.Short:
                    return new JShort((short)reference.Value);
                case RefKind.Int:
                    return new JInt(Ints.Read(reference.Value));
                case RefKind.Lng:
                    return new JLong(Longs.Read(reference.Value));
                case RefKind.BigInt:
                    return new JBigInt(BigInts.Read(reference.Value));
                case RefKind.Flt:
                    return new JSingle(Floats.Read(reference.Value));
                case RefKind.Dbl:
                    return new JDouble(Doubles.Read(reference.Value));
                case RefKind.BigDec:
                    return new JBigDecimal(BigDecimals.Read(reference.Value));
                case RefKind.Str:
                    return new JStr(Strings.Read(reference.Value));
                case RefKind.Arr:
                    return new JArr(Arrs.Read(reference.Value));
                case RefKind.Obj:
                    return new JObj(Objs.Read(reference.Value));
                case RefKind.Root:
                    return new JRoot(Roots.Read(reference.Value));
                default:
                    throw new InvalidDataException($"BUG: Unknown reference: `{reference}`");
            }
        }

        public void Dispose()
        {
            _stream.Close();
        }

        private Header ReadHeader()
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

        public IJsonVal Query(JObj jObj, string path)
        {
            return Query(jObj, path.Split('.').ToList(), jObj, path);
        }

        private IJsonVal Query(JObj jObj, List<string> parts, JObj initialObj, string initialQuery)
        {
            if (parts.Count == 0)
            {
                return jObj;
            }

            var currentQuery = parts.First();
            var next = parts.Skip(1).ToList();
            HandleBracketsWithoutDot(ref currentQuery, next);
            var resolvedObj = ReadObjectFieldRef(currentQuery, jObj.Value,
                $"query `{initialQuery}` on object `{initialObj}`");
            return Query(resolvedObj, next);
        }


        private static void HandleBracketsWithoutDot(ref string currentQuery, List<string> next)
        {
            if (currentQuery.EndsWith(']') && currentQuery.Contains('[') && !currentQuery.StartsWith('['))
            {
                var index = currentQuery.Substring(currentQuery.IndexOf('['));
                currentQuery = currentQuery.Substring(0, currentQuery.IndexOf('['));
                next.Insert(0, index);
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
    }
}