using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SickSharp.Primitives
{
    public static class ByteExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16BE(this byte[] arr, int start)
        {
            return (ushort)((arr[start] << 8) | arr[start + 1]);

            // if (BitConverter.IsLittleEndian)
            // {
            //     return (ushort)((arr[start] << 8) | arr[start + 1]);
            // }
            // return (ushort)(arr[start] | (arr[start + 1] << 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16BE(this ReadOnlySpan<byte> bytes)
        {
            Debug.Assert(bytes.Length == sizeof(short));
            return BinaryPrimitives.ReadUInt16BigEndian(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16BE(this byte[] bytes)
        {
            Debug.Assert(bytes.Length == sizeof(short));
            return BinaryPrimitives.ReadUInt16BigEndian(bytes);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32BE(this byte[] arr, int start)
        {
            return arr[start] << 24 | (arr[start + 1] << 16) | (arr[start + 2] << 8) | arr[start + 3];

            // if (BitConverter.IsLittleEndian)
            // {
            //     return arr[start] << 24 | (arr[start + 1] << 16) | (arr[start + 2] << 8) | arr[start + 3];
            // }
            //
            // return arr[start] | (arr[start + 1] << 8) | (arr[start + 2] << 16) | (arr[start + 3] << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32BE(this ReadOnlySpan<byte> bytes)
        {
            Debug.Assert(bytes.Length == sizeof(int));
            return BinaryPrimitives.ReadInt32BigEndian(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32BE(this byte[] bytes)
        {
            Debug.Assert(bytes.Length == sizeof(int));
            return BinaryPrimitives.ReadInt32BigEndian(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16BE(this ReadOnlySpan<byte> bytes)
        {
            Debug.Assert(bytes.Length == sizeof(short));
            return BinaryPrimitives.ReadInt16BigEndian(bytes);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64BE(this ReadOnlySpan<byte> bytes)
        {
            Debug.Assert(bytes.Length == sizeof(long));
            return BinaryPrimitives.ReadInt64BigEndian(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloatBE(this ReadOnlySpan<byte> bytes)
        {
            // return BinaryPrimitives.ReadSingleBigEndian(bytes); // not supported on mono yet o_O
            Debug.Assert(bytes.Length == sizeof(float));
            return BitConverter.IsLittleEndian ? BitConverter.Int32BitsToSingle(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<int>(bytes))) : MemoryMarshal.Read<float>(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDoubleBE(this ReadOnlySpan<byte> bytes)
        {
            Debug.Assert(bytes.Length == sizeof(double));
            // return BinaryPrimitives.ReadDoubleBigEndian(bytes);  // not supported on mono yet o_O

            return BitConverter.IsLittleEndian ? BitConverter.Int64BitsToDouble(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<long>(bytes))) : MemoryMarshal.Read<double>(bytes);
        }
    }
}