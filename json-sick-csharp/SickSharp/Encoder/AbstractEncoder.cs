using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using SickSharp.Format.Tables;
using SickSharp.Primitives;

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
        public static readonly IFixedByteEncoder<SByte> ByteEncoder = new ByteEncoder();
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
    
    class ByteEncoder : IFixedByteEncoder<SByte>
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
            return Fixed.ByteEncoder.Bytes((sbyte)value);
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
            return (new List<byte[]> { Fixed.RefKindEncoder.Bytes(value.Kind), Fixed.IntEncoder.Bytes(value.Value) }).Concatenate();
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
            return (new List<byte[]> { Fixed.IntEncoder.Bytes(value.Key), Fixed.RefEncoder.Bytes(value.Value),  }).Concatenate();
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
            return (new List<byte[]> { Fixed.IntEncoder.Bytes(value.Key), Fixed.RefEncoder.Bytes(value.Reference),  }).Concatenate();
        }

        public int BlobSize()
        {
            return 1 + 2 * Fixed.IntEncoder.BlobSize();
        }
    }

    
    public interface IFixedArrayByteEncoder<T> : IByteEncoder<T>
    {
        public int ElementSize();
    }

    class FixedArrayByteEncoder<T> : IFixedArrayByteEncoder<List<T>>
    {
        private IFixedByteEncoder<T> _elementEncoder;

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
    class RefListEncoder : IFixedArrayByteEncoder<List<Ref>>
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
    class ObjListEncoder : IFixedArrayByteEncoder<List<ObjEntry>>
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
    class RootListEncoder : IFixedArrayByteEncoder<List<Root>>
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
        public static IFixedArrayByteEncoder<List<ObjEntry>> ObjListEncoder = new ObjListEncoder();
        public static IFixedArrayByteEncoder<List<Ref>> RefListEncoder = new RefListEncoder();
        public static IFixedArrayByteEncoder<List<Root>> RootListEncoder = new RootListEncoder();
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
            var elements = value.Select(e => _elementEncoder.Bytes(e)).ToList();
            var offsets = elements.ComputeOffsets(0);
            var header = offsets.Prepend(value.Count).Select(o => Fixed.IntEncoder.Bytes(o)).Concatenate();
            var lastOffset = !offsets.Any() ? 0 : offsets.Last() + elements.Last().Length;
            return elements.Prepend(Fixed.IntEncoder.Bytes(lastOffset)).Prepend(header).Concatenate();
        }
    }

    class FixedArrayEncoder<T> : IVarArrayByteEncoder<T>
    {
        private IFixedArrayByteEncoder<T> _elementEncoder;

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