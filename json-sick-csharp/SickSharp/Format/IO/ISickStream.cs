using System;
using System.IO;
using System.Runtime.CompilerServices;
using SickSharp.Primitives;

// ReSharper disable InconsistentNaming

namespace SickSharp.IO
{
    public interface ISickStream : IDisposable
    {
        public ReadOnlySpan<byte> ReadSpan(int offset, int count);
        public ReadOnlyMemory<byte> ReadMemory(int offset, int count);
        public void Close();

        public sealed class Cached : ISickStream
        {
            private readonly ISickCacheManager _cacheManager;
            private readonly PageCachedFile _cachedFile;
            private readonly PageCachedStream _cachedStream;

            private readonly object _locker = new { };
            private volatile bool _closed;

            public Cached(ISickCacheManager cacheManager, string filePath, int cachePageSize, ISickProfiler profiler)
            {
                _cacheManager = cacheManager;
                _cachedFile = cacheManager.Acquire(filePath, cachePageSize, profiler);
                _cachedStream = new PageCachedStream(_cachedFile);
            }

            ~Cached()
            {
                Dispose();
            }

            public ReadOnlySpan<byte> ReadSpan(int offset, int count)
            {
                return _cachedStream.ReadSpan(offset, count);
            }

            public ReadOnlyMemory<byte> ReadMemory(int offset, int count)
            {
                return _cachedStream.ReadMemory(offset, count);
            }

            public void Dispose()
            {
                Close();
            }

            public void Close()
            {
                if (_closed) return;
                lock (_locker)
                {
                    if (_closed) return;
                    _closed = true;
                    _cachedStream.Close();
                    _cacheManager.Return(_cachedFile);
                }
            }
        }

        public sealed class Buffer : ISickStream
        {
            private readonly byte[] _buffer;

            public Buffer(byte[] buffer)
            {
                _buffer = buffer;
            }

            public ReadOnlySpan<byte> ReadSpan(int offset, int count)
            {
                var ret = new ReadOnlySpan<byte>(_buffer, offset, count);
                return ret;
            }

            public ReadOnlyMemory<byte> ReadMemory(int offset, int count)
            {
                var ret = new ReadOnlyMemory<byte>(_buffer, offset, count);
                return ret;
            }

            public void Dispose()
            {
            }

            public void Close()
            {
            }
        }

        public sealed class Default : ISickStream
        {
            private readonly Stream _stream;

            public Default(Stream stream)
            {
                _stream = stream;
            }

            public ReadOnlySpan<byte> ReadSpan(int offset, int count)
            {
                return new ReadOnlySpan<byte>(_stream.ReadBufferUnsafe(offset, count));
            }

            public ReadOnlyMemory<byte> ReadMemory(int offset, int count)
            {
                return new ReadOnlyMemory<byte>(_stream.ReadBufferUnsafe(offset, count));
            }

            public void Dispose()
            {
                _stream.Dispose();
            }

            public void Close()
            {
                _stream.Close();
            }
        }

        public sealed class DefaultWithLock : ISickStream
        {
            private readonly object locker = new { };
            private readonly Stream _stream;

            public DefaultWithLock(Stream stream)
            {
                _stream = stream;
            }

            public ReadOnlySpan<byte> ReadSpan(int offset, int count)
            {
                lock (locker)
                {
                    return new ReadOnlySpan<byte>(_stream.ReadBufferUnsafe(offset, count));
                }
            }

            public ReadOnlyMemory<byte> ReadMemory(int offset, int count)
            {
                lock (locker)
                {
                    return new ReadOnlyMemory<byte>(_stream.ReadBufferUnsafe(offset, count));
                }
            }

            public void Dispose()
            {
                lock (locker)
                {
                    _stream.Dispose();
                }
            }

            public void Close()
            {
                lock (locker)
                {
                    _stream.Close();
                }
            }
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
    }
}