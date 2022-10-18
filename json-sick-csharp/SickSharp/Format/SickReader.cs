#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    public class SickReader
    {
        private readonly Dictionary<string, Ref> _roots = new();
        private readonly Stream _stream;

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
            Objs = new ObjTable(_stream, Strings, (UInt32)Header.Offsets[8]);
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

        public Ref? GetRoot(string id)
        {
            Ref? value;
            return _roots.TryGetValue(id, out value) ? value : null;
        }

        public IJsonVal Query(Ref reference, string path)
        {
            return Query(reference, path.Split('.').ToList());
        }

        private IJsonVal Query(Ref reference, List<string> parts)
        {
            var value = Resolve(reference);
            if (value == null)
            {
                throw new KeyNotFoundException(
                    $"Reference {reference} not found with query {String.Join("->", parts)}"
                    );
            }

            if (parts.Count == 0)
            {
                return value;
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
                if (value is JArr)
                {
                    var currentObj = ((JArr)value).Value;
                    var i = (iindex >= 0)?iindex : currentObj.Count + iindex;
                    return Query(currentObj.Read(i), next);
                }
                
                throw new KeyNotFoundException(
                    $"Tried to find element {iindex} in entity with id {reference} which should be an array, but it was {value}"
                );
            }

            if (value is JObj)
            {
                var currentObj = ((JObj)value).Value;

                var lower = 0;
                var upper = currentObj.Count;
                    
                if (currentObj.UseIndex)
                {
                    var khash = KHash.Compute(currentQuery);
                    var bucket = Convert.ToUInt32(khash / OneObjTable.BucketSize);

                    var probablyLower = currentObj.Index[bucket];
                    if (probablyLower == OneObjTable.NoIndex)
                    {
                        throw new KeyNotFoundException(
                            $"Field {currentQuery} not found in object {currentObj} with id {reference}"
                        );
                    }
                        
                    if (probablyLower < OneObjTable.MaxIndex)
                    {
                        // Console.WriteLine($"{currentQuery} {khash} {khash / OneObjTable.BucketSize};; {probablyLower} {currentObj.NextIndex[probablyLower]}");
                        lower = probablyLower;
                        upper = currentObj.NextIndex[probablyLower];
                    }
                }
                    
                for (int i = lower; i < upper; i++)
                {
                    var k = currentObj.ReadKey(i);
                    if (k.Key == currentQuery)
                    {
                        return Query(k.Value, next);
                    }
                }
                throw new KeyNotFoundException(
                    $"Field {currentQuery} not found in object {currentObj} with id {reference}"
                );
            }

            throw new KeyNotFoundException(
                $"Tried to find field {currentQuery} in entity with id {reference} which should be an object, but it was {value}"
            );

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

        ~SickReader()
        {
            _stream.Close();
        }


        private Header ReadHeader()
        {
            _stream.Position = 0;

            var version = _stream.ReadInt32();
            var tableCount = _stream.ReadInt32();

            Debug.Assert(version == 0);
            Debug.Assert(tableCount == 10);

            var tableOffsets = new List<int>();

            foreach (var t in Enumerable.Range(0, tableCount))
            {
                var next = _stream.ReadInt32();
                if (t > 0) Debug.Assert(next > tableOffsets[t - 1]);
                tableOffsets.Add(next);
            }

            var header = new Header(version, tableCount, tableOffsets);
            return header;
        }
    }
}