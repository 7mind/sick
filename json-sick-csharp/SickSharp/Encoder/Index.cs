using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SickSharp.Format.Tables;
using SickSharp.Primitives;

namespace SickSharp.Encoder
{
    public class Index
    {
        private Bijection<Byte> _bytes;
        private Bijection<Int16> _shorts;
        private Bijection<Int32> _ints;
        private Bijection<Int64> _longs;
        private Bijection<BigInteger> _bigints;
        
        private Bijection<Single> _floats;
        private Bijection<Double> _doubles;
        private Bijection<BigDecimal> _bigDecs;
        
        private Bijection<String> _strings;
        private Bijection<List<Ref>> _arrs;
        private Bijection<List<ObjEntry>> _objs;
        private Bijection<List<Root>> _roots;
        
        public Index(Bijection<byte> bytes, Bijection<short> shorts, Bijection<int> ints, Bijection<long> longs, Bijection<BigInteger> bigints, Bijection<float> floats, Bijection<double> doubles, Bijection<BigDecimal> bigDecs, Bijection<string> strings, Bijection<List<Ref>> arrs, Bijection<List<ObjEntry>> objs, Bijection<List<Root>> roots)
        {
            _bytes = bytes;
            _shorts = shorts;
            _ints = ints;
            _longs = longs;
            _floats = floats;
            _doubles = doubles;
            _bigDecs = bigDecs;
            _strings = strings;
            _arrs = arrs;
            _objs = objs;
            _roots = roots;
            _bigints = bigints;
        }

        public static Index Create()
        {
            return new Index(
                Bijection<byte>.Create("bytes"),
                Bijection<short>.Create("shorts"),
                Bijection<int>.Create("ints"),
                Bijection<long>.Create("longs"),
                Bijection<BigInteger>.Create("bigints"),
                Bijection<Single>.Create("floats"),
                Bijection<Double>.Create("doubles"),
                Bijection<BigDecimal>.Create("bigdecs"),
                Bijection<String>.Create("strings"),
                Bijection<List<Ref>>.Create("arrays"),
                Bijection<List<ObjEntry>>.Create("objects"),
                Bijection<List<Root>>.Create("roots")
                );
        }

        public List<SerializedTable> SerializedTables()
        {
            return new List<SerializedTable>
            {
                new(_bytes.Name, new FixedArrayByteEncoder<byte>(Fixed.ByteEncoder).Bytes(_bytes.AsList())),
                new(_shorts.Name, new FixedArrayByteEncoder<short>(Fixed.ShortEncoder).Bytes(_shorts.AsList())),
                new(_ints.Name, new FixedArrayByteEncoder<int>(Fixed.IntEncoder).Bytes(_ints.AsList())),
                new(_longs.Name, new FixedArrayByteEncoder<long>(Fixed.LongEncoder).Bytes(_longs.AsList())),
                new(_bigints.Name, new VarArrayEncoder<BigInteger>(Variable.BigIntEncoder).Bytes(_bigints.AsList())),
                new(_floats.Name, new FixedArrayByteEncoder<float>(Fixed.FloatEncoder).Bytes(_floats.AsList())),
                new(_doubles.Name, new FixedArrayByteEncoder<double>(Fixed.DoubleEncoder).Bytes(_doubles.AsList())),
                new(_bigDecs.Name, new VarArrayEncoder<BigDecimal>(Variable.BigDecimalEncoder).Bytes(_bigDecs.AsList())),
                new(_strings.Name, new VarArrayEncoder<string>(Variable.StringEncoder).Bytes(_strings.AsList())),
                new(_arrs.Name,  new FixedArrayEncoder<List<Ref>>(FixedArray.RefListEncoder).Bytes(_arrs.AsList())),
                new(_objs.Name,  new FixedArrayEncoder<List<ObjEntry>>(FixedArray.ObjListEncoder).Bytes(_objs.AsList())),
                new(_roots.Name,  new FixedArrayEncoder<List<Root>>(FixedArray.RootListEncoder).Bytes(_roots.AsList())),
            };
        }

        public SerializedIndex Serialize()
        {
            var version = 0;
            var tables = SerializedTables().Map(d => d.data).ToList();
            var headerLen = (2 + tables.Count) * Fixed.IntEncoder.BlobSize();
            var offsets = tables.ComputeOffsets(headerLen);

            var encodedOffsets = new FixedArrayByteEncoder<int>(Fixed.IntEncoder).Bytes(offsets);
            var header = new List<byte[]> {
                Fixed.IntEncoder.Bytes(version),
            } ;
            var everything = (header.Append(encodedOffsets).Append(tables).ToList()).Merge();
            return new SerializedIndex(everything);
        }
    }

    public record SerializedIndex(byte[] data);
    public record SerializedTable(string name, byte[] data);

}