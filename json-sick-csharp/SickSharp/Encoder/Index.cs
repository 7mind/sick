using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;
using SickSharp.Format.Tables;
using SickSharp.Primitives;

namespace SickSharp.Encoder
{
    public class Index
    {
        private Bijection<Int32> _ints;
        private Bijection<Int64> _longs;
        private Bijection<BigInteger> _bigints;
        
        private Bijection<Single> _floats;
        private Bijection<Double> _doubles;
        private Bijection<BigDecimal> _bigDecs;
        
        private Bijection<String> _strings;
        private Bijection<List<SickRef>> _arrs;
        private Bijection<List<ObjEntry>> _objs;
        private Bijection<SickRoot> _roots;
        public readonly ObjIndexing Settings;

        public Index(
            Bijection<int> ints, 
            Bijection<long> longs, 
            Bijection<BigInteger> bigints, 
            Bijection<float> floats, 
            Bijection<double> doubles, 
            Bijection<BigDecimal> bigDecs, 
            Bijection<string> strings, 
            Bijection<List<SickRef>> arrs, 
            Bijection<List<ObjEntry>> objs, 
            Bijection<SickRoot> roots, ObjIndexing settings)
        {
            // _bytes = bytes;
            // _shorts = shorts;
            _ints = ints;
            _longs = longs;
            _floats = floats;
            _doubles = doubles;
            _bigDecs = bigDecs;
            _strings = strings;
            _arrs = arrs;
            _objs = objs;
            _roots = roots;
            Settings = settings;
            _bigints = bigints;
        }

        public static Index Create(ushort buckets = 128, ushort limit = 2)
        {
            return new Index(
                Bijection<int>.Create("ints", null),
                Bijection<long>.Create("longs", null),
                Bijection<BigInteger>.Create("bigints", null),
                Bijection<Single>.Create("floats", null),
                Bijection<Double>.Create("doubles", null),
                Bijection<BigDecimal>.Create("bigdecs", null),
                Bijection<String>.Create("strings", null),
                Bijection<List<SickRef>>.Create("arrays", new ListComparer<SickRef>()),
                Bijection<List<ObjEntry>>.Create("objects", new ListComparer<ObjEntry>()),
                Bijection<SickRoot>.Create("roots", null),
                new ObjIndexing(buckets, limit)
                );
        }

        public List<SerializedTable> SerializedTables()
        {
            return new List<SerializedTable>
            {
                new(_ints.Name, new FixedArrayByteEncoder<int>(Fixed.IntEncoder).Bytes(_ints.AsList())),
                new(_longs.Name, new FixedArrayByteEncoder<long>(Fixed.LongEncoder).Bytes(_longs.AsList())),
                new(_bigints.Name, new VarArrayEncoder<BigInteger>(Variable.BigIntEncoder).Bytes(_bigints.AsList())),
                new(_floats.Name, new FixedArrayByteEncoder<float>(Fixed.FloatEncoder).Bytes(_floats.AsList())),
                new(_doubles.Name, new FixedArrayByteEncoder<double>(Fixed.DoubleEncoder).Bytes(_doubles.AsList())),
                new(_bigDecs.Name, new VarArrayEncoder<BigDecimal>(Variable.BigDecimalEncoder).Bytes(_bigDecs.AsList())),
                new(_strings.Name, new VarArrayEncoder<string>(Variable.StringEncoder).Bytes(_strings.AsList())),
                new(_arrs.Name,  new FixedArrayEncoder<List<SickRef>>(FixedArray.RefListEncoder).Bytes(_arrs.AsList())),
                new(_objs.Name,  new FixedArrayEncoder<List<ObjEntry>>(FixedArray.ObjListEncoder(_strings, Settings)).Bytes(_objs.AsList())),
                new(_roots.Name,  FixedArray.RootListEncoder.Bytes(_roots.AsList())),
            };
        }

        public SerializedIndex Serialize()
        {
            var version = 0;
            var tables = SerializedTables().Select(d => d.data).ToList();
            var headerLen = (2 + tables.Count) * Fixed.IntEncoder.BlobSize() + Fixed.UInt16Encoder.BlobSize();
            var offsets = tables.ComputeOffsets(headerLen);

            
            var header = new List<byte[]> {
                Fixed.IntEncoder.Bytes(version),
                new FixedArrayByteEncoder<int>(Fixed.IntEncoder).Bytes(offsets),
                Fixed.UInt16Encoder.Bytes(Settings.BucketCount),
            } ;

            var everything = (header.Concat(tables).ToList()).Concatenate();
            return new SerializedIndex(everything);
        }

        // this can be externalized so Index won't depend on json.net
        public SickRef append(String id, JToken json)
        {
            var idRef = addString(id);
            var rootRef = traverse(json);
            var root = new SickRoot(idRef.Value, rootRef);
            return _roots.RevGet(root).Match(
                Some: some => throw new InvalidDataException($"Cannot find root '{root}'"), 
                None: () => new SickRef(SickKind.Root, _roots.Add(root))
                );
        }

        private SickRef traverse(JToken json)
        {
            return json switch
            {
                JObject v =>
                    addObj(
                        v.Properties().Select(e => new ObjEntry(addString(e.Name).Value, traverse(e.Value))).ToList()
                        ),
                JArray v =>
                    addArr(v.Select(e => traverse(e)).ToList()),
                JValue v =>
                    v.Type switch
                    {
                        JTokenType.Integer => handleInt(v),
                        JTokenType.Float => handleFloat(v),
                        JTokenType.String => addString((string)v.Value),
                        JTokenType.Boolean => new SickRef(SickKind.Bit, Convert.ToInt32((bool)v.Value)),
                        JTokenType.Null => new SickRef(SickKind.Null, 0),
                        JTokenType.Date => addString(((DateTime)v.Value).ToString()),
                        _ => throw new NotImplementedException($"BUG: unknown value `{v}`")
                    ,
                    },
                _ => 
                    throw new NotImplementedException($"BUG: unknown token `{json}`"),
            };
        }

        private SickRef handleInt(JValue v)
        {
            long val;
            switch (v.Value)
            {
                case Int64 int64:
                    val = int64;
                    break;
                case Int32 int32:
                    val = int32;
                    break;
                case uint uint32:
                    val = (long)uint32;
                    break;
                case ulong uint64:
                    val = (long)uint64;
                    break;
                case short int16:
                    val = int16;
                    break;
                case ushort uint16:
                    val = uint16;
                    break;
                case BigInteger bigint:
                    return addBigInt(bigint);
                case BigDecimal bigDecimal:
                    return addBigDec(bigDecimal);
                default:
                    throw new InvalidDataException($"BUG: Unexpected integer: `{v.Value}`");
            }

            if (val <= SByte.MaxValue && val >= SByte.MinValue)
            {
                return new SickRef(SickKind.SByte, (sbyte)val);
            }

            if (val <= Int16.MaxValue && val >= Int16.MinValue)
            {
                return new SickRef(SickKind.Short, (short)val);
            }

            if (val <= Int32.MaxValue && val >= Int32.MinValue)
            {
                return addInt(Convert.ToInt32(val));
            }

            return addLong(val);
        }

        private SickRef handleFloat(JValue v)
        {
            double val;
            switch (v.Value)
            {
                case float f:
                    val = f;
                    break;
                case double d:
                    val = d;
                    break;
                // We can't ever get BigDecimal values from Json.NET because it doesn't support them:
                // https://stackoverflow.com/questions/38864934/how-do-i-deserialize-a-high-precision-decimal-value-with-json-net
                default:
                    throw new InvalidDataException($"BUG: Unexpected float: `{v}`");
            }

            if (val <= Single.MaxValue && val >= Single.MinValue)
            {
                return addFloat(Convert.ToSingle(val));
            }

            return addDouble(val);
        }

        private SickRef addString(String s)
        {
            return new SickRef(SickKind.String, _strings.Add(s));
        }
        
        private SickRef addInt(Int32 s)
        {
            return new SickRef(SickKind.Int, _ints.Add(s));
        }

        private SickRef addLong(Int64 s)
        {
            return new SickRef(SickKind.Long, _longs.Add(s));
        }

        private SickRef addBigInt(BigInteger s)
        {
            return new SickRef(SickKind.BigInt, _bigints.Add(s));
        }

        private SickRef addFloat(Single s)
        {
            return new SickRef(SickKind.Float, _floats.Add(s));
        }

        private SickRef addDouble(Double s)
        {
            return new SickRef(SickKind.Double, _doubles.Add(s));
        }

        private SickRef addBigDec(BigDecimal s)
        {
            return new SickRef(SickKind.BigDec, _bigDecs.Add(s));
        }

        private SickRef addArr(List<SickRef> s)
        {
            return new SickRef(SickKind.Array, _arrs.Add(s));
        }

        private SickRef addObj(List<ObjEntry> s)
        {
            return new SickRef(SickKind.Object, _objs.Add(s));
        }
    }

    public record SerializedIndex(byte[] data);
    public record SerializedTable(string name, byte[] data);

}