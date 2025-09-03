using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using SickSharp.Format.Tables;
using SickSharp.Primitives;

[assembly: InternalsVisibleTo("SickSharp.Test")]

namespace SickSharp.Encoder
{
    internal interface IByteEncoder<T>
    {
        public byte[] Bytes(T value);
    }

    internal interface IFixedByteEncoder<T> : IByteEncoder<T>
    {
        public int BlobSize();
    }

    internal static class Fixed
    {
        public static readonly IFixedByteEncoder<sbyte> ByteEncoder = new ByteEncoder();
        public static readonly IFixedByteEncoder<short> ShortEncoder = new ShortEncoder();
        public static readonly IFixedByteEncoder<int> IntEncoder = new IntEncoder();
        public static readonly IFixedByteEncoder<ushort> UInt16Encoder = new UInt16Encoder();
        public static readonly IFixedByteEncoder<long> LongEncoder = new LongEncoder();
        public static readonly IFixedByteEncoder<float> FloatEncoder = new FloatEncoder();
        public static readonly IFixedByteEncoder<double> DoubleEncoder = new DoubleEncoder();
        public static readonly IFixedByteEncoder<SickKind> RefKindEncoder = new RefKindEncoder();
        public static readonly IFixedByteEncoder<SickRef> RefEncoder = new RefEncoder();
        public static readonly IFixedByteEncoder<ObjEntry> ObjEntryEncoder = new ObjEntryEncoder();
        public static readonly IFixedByteEncoder<SickRoot> RootEncoder = new RootEncoder();
    }

    internal class ByteEncoder : IFixedByteEncoder<sbyte>
    {
        public byte[] Bytes(sbyte value)
        {
            return new byte[1] { (byte)value };
        }

        public int BlobSize()
        {
            return 1;
        }
    }

    internal class ShortEncoder : IFixedByteEncoder<short>
    {
        public byte[] Bytes(short value)
        {
            return BitConverter.IsLittleEndian ? BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(value)) : BitConverter.GetBytes(value);
        }

        public int BlobSize()
        {
            return 2;
        }
    }

    internal class IntEncoder : IFixedByteEncoder<int>
    {
        public byte[] Bytes(int value)
        {
            return BitConverter.IsLittleEndian ? BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(value)) : BitConverter.GetBytes(value);
        }

        public int BlobSize()
        {
            return 4;
        }
    }

    internal class UInt16Encoder : IFixedByteEncoder<ushort>
    {
        public byte[] Bytes(ushort value)
        {
            return BitConverter.IsLittleEndian ? BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(value)) : BitConverter.GetBytes(value);
        }

        public int BlobSize()
        {
            return 2;
        }
    }

    internal class LongEncoder : IFixedByteEncoder<long>
    {
        public byte[] Bytes(long value)
        {
            return BitConverter.IsLittleEndian ? BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(value)) : BitConverter.GetBytes(value);
        }

        public int BlobSize()
        {
            return 8;
        }
    }

    internal class FloatEncoder : IFixedByteEncoder<float>
    {
        public byte[] Bytes(float value)
        {
            var asint = BitConverter.SingleToInt32Bits(value);
            return Fixed.IntEncoder.Bytes(asint);
        }

        public int BlobSize()
        {
            return 32;
        }
    }

    internal class DoubleEncoder : IFixedByteEncoder<double>
    {
        public byte[] Bytes(double value)
        {
            var asint = BitConverter.DoubleToInt64Bits(value);
            return Fixed.LongEncoder.Bytes(asint);
        }

        public int BlobSize()
        {
            return 64;
        }
    }

    internal class RefKindEncoder : IFixedByteEncoder<SickKind>
    {
        public byte[] Bytes(SickKind value)
        {
            return Fixed.ByteEncoder.Bytes((sbyte)value);
        }

        public int BlobSize()
        {
            return 1;
        }
    }

    internal class RefEncoder : IFixedByteEncoder<SickRef>
    {
        public byte[] Bytes(SickRef value)
        {
            return (new List<byte[]> { Fixed.RefKindEncoder.Bytes(value.Kind), Fixed.IntEncoder.Bytes(value.Value) }).Concatenate();
        }

        public int BlobSize()
        {
            return 1 + Fixed.IntEncoder.BlobSize();
        }
    }

    internal class ObjEntryEncoder : IFixedByteEncoder<ObjEntry>
    {
        public byte[] Bytes(ObjEntry value)
        {
            return (new List<byte[]> { Fixed.IntEncoder.Bytes(value.Key), Fixed.RefEncoder.Bytes(value.Value) }).Concatenate();
        }

        public int BlobSize()
        {
            return 1 + 2 * Fixed.IntEncoder.BlobSize();
        }
    }

    internal class RootEncoder : IFixedByteEncoder<SickRoot>
    {
        public byte[] Bytes(SickRoot value)
        {
            return (new List<byte[]> { Fixed.IntEncoder.Bytes(value.Key), Fixed.RefEncoder.Bytes(value.Reference) }).Concatenate();
        }

        public int BlobSize()
        {
            return 1 + 2 * Fixed.IntEncoder.BlobSize();
        }
    }


    internal interface IFixedArrayByteEncoder<T> : IByteEncoder<T>
    {
        public int ElementSize();
    }

    internal class FixedArrayByteEncoder<T> : IFixedArrayByteEncoder<List<T>>
    {
        private readonly IFixedByteEncoder<T> _elementEncoder;

        public FixedArrayByteEncoder(IFixedByteEncoder<T> elementEncoder)
        {
            _elementEncoder = elementEncoder;
        }

        public byte[] Bytes(List<T> value)
        {
            return value.Select(e => _elementEncoder.Bytes(e)).Prepend(Fixed.IntEncoder.Bytes(value.Count))
                .Concatenate();
        }

        public int ElementSize()
        {
            return _elementEncoder.BlobSize();
        }
    }

    internal class RefListEncoder : IFixedArrayByteEncoder<List<SickRef>>
    {
        public byte[] Bytes(List<SickRef> value)
        {
            return new FixedArrayByteEncoder<SickRef>(Fixed.RefEncoder).Bytes(value);
        }

        public int ElementSize()
        {
            return Fixed.RefEncoder.BlobSize();
        }
    }

    internal class ObjListEncoder : IFixedArrayByteEncoder<List<ObjEntry>>
    {
        private readonly Bijection<string> _strings;
        private readonly ObjIndexing _settings;

        public ObjListEncoder(Bijection<string> strings, ObjIndexing settings)
        {
            _strings = strings;
            _settings = settings;
        }

        public byte[] Bytes(List<ObjEntry> value)
        {
            var index = new List<UInt16>();
            var data = value;

            if (value.Count > _settings.Limit)
            {
                if (value.Count >= ObjIndexing.MaxIndex)
                {
                    throw new ArgumentException($"BUG: Too many values in an object, the limit is {ObjIndexing.MaxIndex}");
                }

                var toIndex = new List<ObjIndexEntry>();
                foreach (var objEntry in value)
                {
                    var kval = _strings.Get(objEntry.Key).UsafeGet();
                    var (hash, bucket) = KHash.Bucket(kval, _settings.BucketSize);
                    toIndex.Add(new ObjIndexEntry(objEntry.Key, objEntry.Value, hash, bucket));
                }

                var ordered = toIndex.OrderBy(obj => obj.Hash).ToList();

                data = ordered.Select(e => new ObjEntry(e.Key, e.Value)).ToList();

                var startIndexes = Enumerable.Repeat(ObjIndexing.MaxIndex, _settings.BucketCount).ToList();

                for (var i = 0; i < ordered.Count; i++)
                {
                    var e = ordered[i];
                    var currentVal = startIndexes[e.Bucket];
                    if (currentVal == ObjIndexing.MaxIndex)
                    {
                        Debug.Assert(i >= 0 && i < ObjIndexing.MaxIndex);
                        startIndexes[e.Bucket] = (ushort)i;
                    }
                }

                ushort last = (ushort)ordered.Count;
                for (var i = _settings.BucketCount - 1; i >= 0; i--)
                {
                    if (startIndexes[i] == ObjIndexing.MaxIndex)
                    {
                        startIndexes[i] = last;
                    }
                    else
                    {
                        last = startIndexes[i];
                    }
                }

                index = startIndexes;
            }
            else
            {
                index.Add(ObjIndexing.NoIndex);
            }

            var elements = new List<byte[]>
            {
                new FixedArrayByteEncoder<UInt16>(Fixed.UInt16Encoder).Bytes(index)[Fixed.IntEncoder.BlobSize()..],
                new FixedArrayByteEncoder<ObjEntry>(Fixed.ObjEntryEncoder).Bytes(data)
            };
            return elements.Concatenate();
        }

        public int ElementSize()
        {
            return Fixed.ObjEntryEncoder.BlobSize();
        }
    }

    internal record ObjIndexEntry(int Key, SickRef Value, long Hash, int Bucket);

    internal class RootListEncoder : IFixedArrayByteEncoder<List<SickRoot>>
    {
        public byte[] Bytes(List<SickRoot> value)
        {
            return new FixedArrayByteEncoder<SickRoot>(Fixed.RootEncoder).Bytes(value);
        }

        public int ElementSize()
        {
            return Fixed.RootEncoder.BlobSize();
        }
    }

    internal static class FixedArray
    {
        public static IFixedArrayByteEncoder<List<ObjEntry>> ObjListEncoder(Bijection<string> strings, ObjIndexing settings)
        {
            return new ObjListEncoder(strings, settings);
        }

        public static readonly IFixedArrayByteEncoder<List<SickRef>> RefListEncoder = new RefListEncoder();
        public static readonly IFixedArrayByteEncoder<List<SickRoot>> RootListEncoder = new RootListEncoder();
    }


    internal interface IVarByteEncoder<T> : IByteEncoder<T>
    {
    }

    internal class StringEncoder : IVarByteEncoder<string>
    {
        public byte[] Bytes(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }
    }

    internal class BigIntEncoder : IVarByteEncoder<BigInteger>
    {
        public byte[] Bytes(BigInteger value)
        {
            return value.ToByteArray(isBigEndian: true);
        }
    }

    internal class BigDecEncoder : IVarByteEncoder<BigDecimal>
    {
        public byte[] Bytes(BigDecimal value)
        {
            var elements = new List<byte[]>
            {
                Fixed.IntEncoder.Bytes(value.Signum),
                Fixed.IntEncoder.Bytes(value.Precision),
                Fixed.IntEncoder.Bytes(value.Scale),
                value.Unscaled.ToByteArray(isBigEndian: true)
            };
            return elements.Concatenate();
        }
    }

    internal static class Variable
    {
        public static readonly IVarByteEncoder<string> StringEncoder = new StringEncoder();
        public static readonly IVarByteEncoder<BigInteger> BigIntEncoder = new BigIntEncoder();
        public static readonly IVarByteEncoder<BigDecimal> BigDecimalEncoder = new BigDecEncoder();
    }

    internal interface IVarArrayByteEncoder<T> : IByteEncoder<List<T>>
    {
    }

    internal class VarArrayEncoder<T> : IVarArrayByteEncoder<T>
    {
        private readonly IVarByteEncoder<T> _elementEncoder;

        public VarArrayEncoder(IVarByteEncoder<T> elementEncoder)
        {
            _elementEncoder = elementEncoder;
        }

        public byte[] Bytes(List<T> value)
        {
            var elements = value.Select(e => _elementEncoder.Bytes(e)).ToList();
            var offsets = elements.ComputeOffsets(0);
            var header = offsets.Prepend(value.Count).Select(o => Fixed.IntEncoder.Bytes(o)).Concatenate();
            var lastOffset = !offsets.Any() ? 0 : offsets.Last() + elements.Last().Length;
            return elements.Prepend(Fixed.IntEncoder.Bytes(lastOffset)).Prepend(header).Concatenate();
        }
    }

    internal class FixedArrayEncoder<T> : IVarArrayByteEncoder<T>
    {
        private readonly IFixedArrayByteEncoder<T> _elementEncoder;

        public FixedArrayEncoder(IFixedArrayByteEncoder<T> elementEncoder)
        {
            _elementEncoder = elementEncoder;
        }


        public byte[] Bytes(List<T> value)
        {
            var elements = value.Select(e => _elementEncoder.Bytes(e)).ToList();
            var offsets = elements.ComputeOffsets(0);
            var header = offsets.Prepend(value.Count).Select(el => Fixed.IntEncoder.Bytes(el)).Concatenate();
            var lastOffset = !offsets.Any() ? 0 : offsets.Last() + elements.Last().Length;
            return elements.Prepend(Fixed.IntEncoder.Bytes(lastOffset)).Prepend(header).Concatenate();
        }
    }
}