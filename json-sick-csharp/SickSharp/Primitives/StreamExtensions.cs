using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace SickSharp.Primitives
{
    public static class StreamExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadBytes(this Stream stream, long at, int size)
        {
            return ReadBuffer(stream, at, size);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadBuffer(this Stream stream, long offset, int count)
        {
            var bytes = new byte[count];
            // stream.Position = offset;
            stream.Seek(offset, SeekOrigin.Begin);
            var ret = stream.Read(bytes, 0, count);
            // Debug.Assert(stream.Length >= stream.Position + count);
            Debug.Assert(ret == count);
            return bytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32BE(this Stream stream, long offset)
        {
            return stream.ReadBuffer(offset, sizeof(int)).ReadInt32BE();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16BE(this Stream stream, long offset)
        {
            return stream.ReadBuffer(offset, sizeof(ushort)).ReadUInt16BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32BE(this Stream stream)
        {
            return ReadInt32BE(stream, stream.Position);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16BE(this Stream stream)
        {
            return ReadUInt16BE(stream, stream.Position);
        }
    }
}