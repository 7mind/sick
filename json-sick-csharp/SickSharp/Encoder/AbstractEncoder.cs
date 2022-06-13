using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using SickSharp.Format.Tables;
using SickSharp.Primitives;
using LanguageExt;
using static LanguageExt.Prelude;

namespace SickSharp.Encoder
{
    public interface IByteEncoder<T>
    {
        public byte[] Bytes(T value);
    }
    
    public interface IFixedByteEncoder<T> : IByteEncoder<T>
    {
        public int BlobSize();
    }

    public class Fixed
    {
        public static readonly IFixedByteEncoder<Byte> ByteEncoder = new ByteEncoder();
        public static IFixedByteEncoder<Int16> ShortEncoder = new ShortEncoder();
        public static readonly IFixedByteEncoder<Int32> IntEncoder = new IntEncoder();
        public static readonly IFixedByteEncoder<Int64> LongEncoder = new LongEncoder();
        public static IFixedByteEncoder<Single> FloatEncoder = new FloatEncoder();
        public static IFixedByteEncoder<Double> DoubleEncoder = new DoubleEncoder();
        public static readonly IFixedByteEncoder<RefKind> RefKindEncoder = new RefKindEncoder();
        public static readonly IFixedByteEncoder<Ref> RefEncoder = new RefEncoder();
        public static IFixedByteEncoder<ObjEntry> ObjEntryEncoder = new ObjEntryEncoder();
        public static IFixedByteEncoder<Root> RootEncoder = new RootEncoder();
    }
    
    class ByteEncoder : IFixedByteEncoder<Byte>
    {
        public byte[] Bytes(byte value)
        {
            return new byte[1] { value };
        }

        public int BlobSize()
        {
            return 1;
        }
    }

    class ShortEncoder : IFixedByteEncoder<Int16>
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

    class IntEncoder : IFixedByteEncoder<Int32>
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

    class LongEncoder : IFixedByteEncoder<Int64>
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
    
    class FloatEncoder : IFixedByteEncoder<Single>
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

    class DoubleEncoder : IFixedByteEncoder<Double>
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

    class RefKindEncoder : IFixedByteEncoder<RefKind>
    {
        public byte[] Bytes(RefKind value)
        {
            return Fixed.ByteEncoder.Bytes((byte)value);
        }

        public int BlobSize()
        {
            return 1;
        }
    }

    class RefEncoder : IFixedByteEncoder<Ref>
    {
        public byte[] Bytes(Ref value)
        {
            return (new List<byte[]> { Fixed.RefKindEncoder.Bytes(value.Kind), Fixed.IntEncoder.Bytes(value.Value) }).Merge();
        }

        public int BlobSize()
        {
            return 1 + Fixed.IntEncoder.BlobSize();
        }
    }
    
    class ObjEntryEncoder : IFixedByteEncoder<ObjEntry>
    {
        public byte[] Bytes(ObjEntry value)
        {
            return (new List<byte[]> { Fixed.IntEncoder.Bytes(value.Key), Fixed.RefEncoder.Bytes(value.Value),  }).Merge();
        }

        public int BlobSize()
        {
            return 1 + 2 * Fixed.IntEncoder.BlobSize();
        }
    }
    class RootEncoder : IFixedByteEncoder<Root>
    {
        public byte[] Bytes(Root value)
        {
            return (new List<byte[]> { Fixed.IntEncoder.Bytes(value.Key), Fixed.RefEncoder.Bytes(value.Reference),  }).Merge();
        }

        public int BlobSize()
        {
            return 1 + 2 * Fixed.IntEncoder.BlobSize();
        }
    }

    
    public interface IFixedArrayByteEncoder<T> : IByteEncoder<List<T>>
    {
        public int ElementSize();
    }

    class FixedArrayByteEncoder<T> : IFixedArrayByteEncoder<T>
    {
        private IFixedByteEncoder<T> _elementEncoder;

        public FixedArrayByteEncoder(IFixedByteEncoder<T> elementEncoder)
        {
            _elementEncoder = elementEncoder;
        }

        public byte[] Bytes(List<T> value)
        {
            return value.Map(e => _elementEncoder.Bytes(e)).Fold(Fixed.IntEncoder.Bytes(value.Count),
                (bytes, bytes1) => bytes.Concatenate(bytes1));
        }

        public int ElementSize()
        {
            return _elementEncoder.BlobSize();
        }
    }
    class RefListEncoder : IFixedArrayByteEncoder<Ref>
    {
        public byte[] Bytes(List<Ref> value)
        {
            return new FixedArrayByteEncoder<Ref>(Fixed.RefEncoder).Bytes(value);
        }

        public int ElementSize()
        {
            return Fixed.RefEncoder.BlobSize();
        }
    }
    class ObjListEncoder : IFixedArrayByteEncoder<ObjEntry>
    {
        public byte[] Bytes(List<ObjEntry> value)
        {
            return new FixedArrayByteEncoder<ObjEntry>(Fixed.ObjEntryEncoder).Bytes(value);
        }

        public int ElementSize()
        {
            return Fixed.ObjEntryEncoder.BlobSize();
        }
    }
    class RootListEncoder : IFixedArrayByteEncoder<Root>
    {
        public byte[] Bytes(List<Root> value)
        {
            return new FixedArrayByteEncoder<Root>(Fixed.RootEncoder).Bytes(value);
        }

        public int ElementSize()
        {
            return Fixed.RootEncoder.BlobSize();
        }
    }
    
    class FixedArray
    {
        public static IFixedArrayByteEncoder<ObjEntry> ObjListEncoder = new ObjListEncoder();
        public static IFixedArrayByteEncoder<Ref> RefListEncoder = new RefListEncoder();
        public static IFixedArrayByteEncoder<Root> RootListEncoder = new RootListEncoder();
    }


    public interface IVarByteEncoder<T> : IByteEncoder<T>
    {
    }

    class StringEncoder : IVarByteEncoder<string>
    {
        public byte[] Bytes(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }
    }
    class BigIntEncoder: IVarByteEncoder<BigInteger>
    {
        public byte[] Bytes(BigInteger value)
        {
            return value.ToByteArray();
        }
    }

    class BigDecEncoder: IVarByteEncoder<BigDecimal>
    {
        public byte[] Bytes(BigDecimal value)
        {
            throw new NotImplementedException();
        }
    }
    public class Variable
    {
        public static IVarByteEncoder<String> StringEncoder = new StringEncoder();
        public static IVarByteEncoder<BigInteger> BigIntEncoder = new BigIntEncoder();
        public static IVarByteEncoder<BigDecimal> BigDecimalEncoder = new BigDecEncoder();
    }


    public interface IVarArrayByteEncoder<T> : IByteEncoder<List<T>>
    {
    }

    class VarArrayEncoder<T> : IVarArrayByteEncoder<T>
    {
        private IVarByteEncoder<T> _elementEncoder;

        public VarArrayEncoder(IVarByteEncoder<T> elementEncoder)
        {
            _elementEncoder = elementEncoder;
        }

        public byte[] Bytes(List<T> value)
        {
            var elements = value.Map(e => _elementEncoder.Bytes(e)).ToList();
            var offsets = elements.ComputeOffsets(0);
            var header = offsets.Fold(Fixed.IntEncoder.Bytes(value.Count),
                (acc, el) => acc.Concatenate(Fixed.IntEncoder.Bytes(el)));
            var lastOffset = offsets.LastOrNone().Map(offset => offset + elements.Last().Length).IfNone(0);
            var data = elements.Fold(Fixed.IntEncoder.Bytes(lastOffset), (acc, bytes) => acc.Concatenate(bytes));
            return header.Concatenate(data);
        }
    }

    class FixedArrayEncoder<T> : IVarArrayByteEncoder<T>
    {
        private IFixedByteEncoder<T> _elementEncoder;

        public FixedArrayEncoder(IFixedByteEncoder<T> elementEncoder)
        {
            _elementEncoder = elementEncoder;
        }


        public byte[] Bytes(List<T> value)
        {
            var elements = value.Map(e => _elementEncoder.Bytes(e)).ToList();
            var offsets = elements.ComputeOffsets(0);
            var header = offsets.Fold(Fixed.IntEncoder.Bytes(value.Count),
                (acc, el) => acc.Concatenate(Fixed.IntEncoder.Bytes(el)));
            var lastOffset = offsets.LastOrNone().Map(offset => offset + elements.Last().Length).IfNone(0);
            var data = elements.Fold(Fixed.IntEncoder.Bytes(lastOffset), (acc, bytes) => acc.Concatenate(bytes));
            return header.Concatenate(data);
        }
    }

}