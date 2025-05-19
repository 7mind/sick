using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace SickSharp.Primitives
{
    public static class StreamExtensions
    {
        /**
         * Read byte buffer unsafe.
         * THIS METHOD IS NOT THREAD SAFE, AND MIGHT BROKE STREAM POSITION ON CONCURRENT ACCESS.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadBufferUnsafe(this Stream stream, int offset, int count)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadUpTo(this Stream stream, byte[] buffer, int offset, int count)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || (offset + count) > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));
            if (!stream.CanRead) throw new InvalidOperationException("Stream does not support reading.");

            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (bytesRead == 0) return totalRead;
                totalRead += bytesRead;
            }

            return totalRead;
        }
    }
}