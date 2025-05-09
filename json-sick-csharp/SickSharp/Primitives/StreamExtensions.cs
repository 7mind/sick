using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace SickSharp.Primitives
{
    using System.IO;


    public static class StreamExtensions
    {
        public static byte[] ReadBytes(this Stream stream, long at, int size)
        {
            return ReadBuffer(stream, at, size);
        }
        
        public static byte[] ReadBuffer(this Stream stream, long offset, int count)
        {
            var bytes = new byte[count];
            // stream.Position = offset;
            stream.Seek(offset, SeekOrigin.Begin);
            var ret = stream.ReadUpTo(bytes, 0, count);
            // Debug.Assert(stream.Length >= stream.Position + count);
            Debug.Assert(ret == count);
            return bytes;
        }
        
        // the loop shouldn't be necessary, but the spec is INSANE:
        // https://learn.microsoft.com/en-us/dotnet/api/System.IO.FileStream.Read?view=netstandard-2.1
        // "...An implementation is free to return fewer bytes than requested even if the end of the stream has not been reached..."
        
        public static int ReadUpTo(this Stream stream, byte[] buffer, int offset, int count)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || (offset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (!stream.CanRead)
                throw new InvalidOperationException("Stream does not support reading.");

            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (bytesRead == 0)
                    return totalRead;
                totalRead += bytesRead;
            }

            return totalRead;
        }

        public static int ReadInt32BE(this Stream stream, long offset)
        {
            return stream.ReadBuffer(offset, sizeof(int)).ReadInt32BE();
        }
        
        public static ushort ReadUInt16BE(this Stream stream, long offset)
        {
            return stream.ReadBuffer(offset, sizeof(ushort)).ReadUInt16BE();
        }

        public static int ReadInt32BE(this Stream stream)
        {
            return ReadInt32BE(stream, stream.Position);
        }
        
        public static ushort ReadUInt16BE(this Stream stream)
        {
            return ReadUInt16BE(stream, stream.Position);
        }
    }
}