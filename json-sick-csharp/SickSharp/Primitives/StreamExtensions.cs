using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SickSharp.Primitives
{
    public static class ByteArrayCollectionExtensions
    {
        public static List<int> ComputeOffsets(this List<byte[]> collection, Int32 initial)
        {
                
            var res = new List<int>() {initial};
            var counts = collection.Select(a => a.Length).ToList();
            for (int i = 0; i < counts.Count(); i++)
            {
                res.Add(res.Last() + counts[i]);
            }
            
            res.RemoveAt(res.Count -1);
            Debug.Assert(res.Count == collection.Count);
            return res;
        }
    }

    public static class ArrayListExtention
    {
        public static T[] Concatenate<T>(this IEnumerable<T[]> arrays)
        {
            var sz = arrays.Select(a => a.Length).Sum();
            T[] result = new T[sz];
            var nextOffset = 0;
            foreach (var array in arrays)
            {
                array.CopyTo(result, nextOffset);
                nextOffset += array.Length;
            }
            return result;
        }
    }
    
    // public static class ArrayExtention
    //     {
    //
    //         public static T[] Concatenate<T>(this T[] array1, T[] array2)
    //         {
    //             T[] result = new T[array1.Length + array2.Length];
    //             array1.CopyTo(result, 0);
    //             array2.CopyTo(result, array1.Length);
    //             return result;
    //         }
    //
    //     }
    
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
            stream.Seek(offset, SeekOrigin.Begin);
            var ret = stream.Read(bytes, 0, count);
            // Debug.Assert(stream.Length >= stream.Position + count);
            Debug.Assert(ret == count);
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
        
        public static ushort ReadUInt16(this byte[] bytes)
        {
            Debug.Assert(bytes.Length == sizeof(short));
            return BinaryPrimitives.ReadUInt16BigEndian(bytes);
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