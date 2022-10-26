#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using SickSharp.Format.Tables;
using SickSharp.Primitives;

namespace SickSharp.Format
{
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

        public static SickReader OpenFile(string path, long inMemoryThreshold = 65536)
        {
            var info = new FileInfo(path);
            var loadIntoMemory = info.Length <= inMemoryThreshold;
            if (loadIntoMemory)
            {
                var stream = new MemoryStream(File.ReadAllBytes(path)); 
                return new SickReader(stream);
            }
            else
            {
                var stream = File.Open(path, FileMode.Open);
                return new SickReader(stream);
            }
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
        
        public Ref QueryRef(Ref reference, string path)
        {
            return QueryRef(reference, path.Split('.').ToList());
        }

        public Ref ReadObjectFieldRef(Ref reference, string field)
        {
            if (reference.Kind == RefKind.Obj)
            {
                var currentObj = Objs.Read(reference.Value);

                var lower = 0;
                var upper = currentObj.Count;
                    
                if (currentObj.UseIndex)
                {
                    
                    //BucketStartOffsets = new ushort[settings.BucketCount];
                    //BucketEndOffsets = new Dictionary<uint, ushort>();

                    uint previousBucketStart = 0;
                    
                    var khash = KHash.Compute(field);
                    var bucket = Convert.ToUInt32(khash / Header.Settings.BucketSize);
                    var probablyLower = BucketValue(currentObj, bucket);

                    if (probablyLower == ObjIndexing.MaxIndex)
                    {
                        throw new KeyNotFoundException(
                            $"Field {field} not found in object {currentObj} with id {reference}"
                        );
                    }
                    
                    if (probablyLower >= currentObj.Count)
                    {
                        throw new FormatException(
                            $"Field {field} in object {currentObj} with id {reference} produced bucket index {probablyLower} which is more than object size {currentObj.Count}"
                        );
                    }
                        
                    lower = probablyLower;

                    for (uint i = bucket+1; i < Header.Settings.BucketCount; i++)
                    {
                        var probablyUpper = BucketValue(currentObj, i);
                        
                        if (probablyUpper < currentObj.Count)
                        {
                            upper = probablyUpper;
                            break;
                        }
                        
                        if (probablyUpper == ObjIndexing.MaxIndex)
                        {
                            continue;
                        }
                        
                        if (probablyUpper >= currentObj.Count)
                        {
                            throw new FormatException(
                                $"Field {field} in object {currentObj} with id {reference} produced bucket index {probablyUpper} which is more than object size {currentObj.Count}"
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
                    var k = currentObj.ReadKey(i);
                    if (k.Key == field)
                    {
                        #if DEBUG_TRAVEL
                        TotalTravel += (i - lower);
                        #endif
                        return k.Value;
                    }
                }
                throw new KeyNotFoundException(
                    $"Field {field} not found in object {currentObj} with id {reference}"
                );
            }
            
            throw new KeyNotFoundException(
                $"Tried to find field {field} in entity with id {reference} which should be an object, but it was {reference}"
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
                var i = (iindex >= 0)?iindex : currentObj.Count + iindex;
                return currentObj.Read(i);
            }
            
            throw new KeyNotFoundException(
                $"Tried to find element {iindex} in entity with id {reference} which should be an array, but it was {reference}"
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

            if (currentQuery.EndsWith("]") && currentQuery.Contains("[") && !currentQuery.StartsWith("["))
            {
                var index = currentQuery.Substring(currentQuery.IndexOf('['));
                currentQuery = currentQuery.Substring(0, currentQuery.IndexOf('['));
                next.Insert(0, index);
            }
            
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
                    $"Failed to query '{reference}' lookup result was '{result}' but it failed to resolve. The query was '{String.Join("->", parts)}'"
                    );
            }

            return value;
        }
        
        public IJsonVal Resolve(Ref reference)
        {
            switch (reference.Kind)
            {
                case RefKind.Nul:
                    return new JNull();
                case RefKind.Bit:
                    return new JBool(reference.Value == 1);
                case RefKind.Byte:
                    return new JByte((byte)reference.Value);
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
                    throw new InvalidDataException($"Unknown reference: {reference}");
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
                throw new FormatException($"SICK version expected to be {expectedVersion}, got {version}");
            }

            var expectedTableCount = 10;
            if (tableCount != expectedTableCount)
            {
                throw new FormatException($"SICK table count expected to be {expectedTableCount}, got {tableCount}");
            }

            var tableOffsets = new List<int>();

            foreach (var t in Enumerable.Range(0, tableCount))
            {
                var next = _stream.ReadInt32BE();
                if (t > 0 && next <= tableOffsets[t - 1])
                {
                    throw new FormatException($"SICK: wrong format, {next} expected to be more than {tableOffsets[t - 1]}");
                }
                tableOffsets.Add(next);
            }

            var bucketCount = _stream.ReadUInt16BE();
            
            var header = new Header(version, tableCount, tableOffsets, new ObjIndexing(bucketCount));
            return header;
        }
    }
}