using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using SickSharp.Format;
using SickSharp.IO;

// ReSharper disable InconsistentNaming

namespace SickSharp.Primitives
{
    public abstract class SpanStream : IDisposable
    {
        private readonly Stream _stream;

        private SpanStream(Stream stream)
        {
            _stream = stream;
        }

        public long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public void Close()
        {
            _stream.Close();
        }

        public abstract ReadOnlySpan<byte> ReadSpan(int offset, int count);
        public abstract ReadOnlyMemory<byte> ReadMemory(int offset, int count);

        public static SpanStream Create(Stream stream)
        {
            return stream switch
            {
                NonAllocPageCachedStream cachedStream => new Cached(cachedStream),
                MemoryStream memoryStream when memoryStream.TryGetBuffer(out var buffer) => new Memory(memoryStream, buffer.Array),
                _ => new Default(stream)
            };
        }

        private sealed class Cached : SpanStream
        {
            private readonly NonAllocPageCachedStream _cachedStream;

            public Cached(NonAllocPageCachedStream cachedStream) : base(cachedStream)
            {
                _cachedStream = cachedStream;
            }

            public override ReadOnlySpan<byte> ReadSpan(int offset, int count)
            {
                return _cachedStream.ReadSpan(offset, count);
            }

            public override ReadOnlyMemory<byte> ReadMemory(int offset, int count)
            {
                return _cachedStream.ReadMemory(offset, count);
            }
        }

        private sealed class Memory : SpanStream
        {
            private readonly byte[] _buffer;

            public Memory(MemoryStream stream, byte[] buffer) : base(stream)
            {
                _buffer = buffer;
            }

            public override ReadOnlySpan<byte> ReadSpan(int offset, int count)
            {
                var ret = new ReadOnlySpan<byte>(_buffer, offset, count);
                Position += count;
                return ret;
            }

            public override ReadOnlyMemory<byte> ReadMemory(int offset, int count)
            {
                var ret = new ReadOnlyMemory<byte>(_buffer, offset, count);
                Position += count;
                return ret;
            }
        }

        private sealed class Default : SpanStream
        {
            public Default(Stream stream) : base(stream)
            {
            }

            public override ReadOnlySpan<byte> ReadSpan(int offset, int count)
            {
                return new ReadOnlySpan<byte>(ReadBuffer(offset, count));
            }


            public override ReadOnlyMemory<byte> ReadMemory(int offset, int count)
            {
                return new ReadOnlyMemory<byte>(ReadBuffer(offset, count));
            }

            private byte[] ReadBuffer(int offset, int count)
            {
                var bytes = new byte[count];
                // stream.Position = offset;
                _stream.Seek(offset, SeekOrigin.Begin);
                var ret = _stream.ReadUpTo(bytes, 0, count);
                // Debug.Assert(stream.Length >= stream.Position + count);
                Debug.Assert(ret == count);
                return bytes;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ReadBytes(int offset, int count)
        {
            return ReadSpan(offset, count).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32BE(int offset)
        {
            return ReadSpan(offset, sizeof(int)).ReadInt32BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16BE(int offset)
        {
            return ReadSpan(offset, sizeof(ushort)).ReadUInt16BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32BE()
        {
            return ReadInt32BE((int)_stream.Position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16BE()
        {
            return ReadUInt16BE((int)_stream.Position);
        }
    }

    public static class StreamExtensions
    {
        // the loop shouldn't be necessary, but the spec is INSANE:
        // https://learn.microsoft.com/en-us/dotnet/api/System.IO.FileStream.Read?view=netstandard-2.1
        // "...An implementation is free to return fewer bytes than requested even if the end of the stream has not been reached..."
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