using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SickSharp.Primitives
{
    public static class StreamExtensions
    {
        public static byte[] ReadBuffer(this Stream stream, int count)
        {
            return ReadBuffer(stream, stream.Position, count);
        }

        public static byte[] ReadBuffer(this Stream stream, long offset, int count)
        {
            var bytes = new byte[count];
            stream.Position = offset;
            Debug.Assert(stream.Length >= stream.Position + count);
            stream.Read(bytes);
            return bytes;
        }

        public static int ReadInt32(this Stream stream, long offset)
        {
            return stream.ReadBuffer(offset, sizeof(int)).ReadInt32();
        }

        public static int ReadInt32(this Stream stream)
        {
            return ReadInt32(stream, stream.Position);
        }
    }

    public static class ByteExtensions
    {
        public static short ReadInt16(this byte[] bytes)
        {
            Debug.Assert(bytes.Length == sizeof(short));
            return BinaryPrimitives.ReadInt16BigEndian(bytes);
        }

        public static int ReadInt32(this byte[] bytes)
        {
            Debug.Assert(bytes.Length == sizeof(int));
            return BinaryPrimitives.ReadInt32BigEndian(bytes);
        }

        public static long ReadInt64(this byte[] bytes)
        {
            Debug.Assert(bytes.Length == sizeof(long));
            return BinaryPrimitives.ReadInt64BigEndian(bytes);
        }

        public static float ReadFloat(this byte[] bytes)
        {
            // return BinaryPrimitives.ReadSingleBigEndian(bytes); // not supported on mono yet o_O
            Debug.Assert(bytes.Length == sizeof(float));
            return BitConverter.IsLittleEndian ?
                BitConverter.Int32BitsToSingle(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<int>(bytes))) :
                MemoryMarshal.Read<float>(bytes);
        }

        public static double ReadDouble(this byte[] bytes)
        {
            Debug.Assert(bytes.Length == sizeof(double));
            // return BinaryPrimitives.ReadDoubleBigEndian(bytes);  // not supported on mono yet o_O
            
            return BitConverter.IsLittleEndian ?
                BitConverter.Int64BitsToDouble(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<long>(bytes))) :
                MemoryMarshal.Read<double>(bytes);
        }
    }
}