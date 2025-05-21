#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using SickSharp.Format.Tables;
using SickSharp.IO;
using SickSharp.Primitives;

namespace SickSharp
{
    /// <summary>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>This class is thread unsafe as per it encapsulates a seekable stream!</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Instances produced by this class are only valid while the instance of the class itself is not
    ///                 finalized!
    ///             </description>
    ///         </item>
    ///     </list>
    /// </summary>
    /// <threadsafety instance="false" />
    public sealed partial class SickReader : SickBaseReader, IDisposable
    {
        private readonly object _locker = new { };
        private volatile bool _disposed;

        private readonly IReadOnlyDictionary<string, SickRef> _roots;

        private readonly ISickStream _stream;
        private readonly ISickProfiler _profiler;

        private readonly Header _header;
        private readonly RootTable _root;
        private readonly IntTable _ints;
        private readonly LongTable _longs;
        private readonly BigIntTable _bigIntegers;
        private readonly FloatTable _floats;
        private readonly DoubleTable _doubles;
        private readonly BigDecTable _bigDecimals;
        private readonly StringTable _strings;
        private readonly ArrTable _arrs;
        private readonly ObjTable _objs;

        internal ISickProfiler Profiler => ThrowIfDisposed(_profiler);
        internal Header Header => ThrowIfDisposed(_header);
        internal RootTable Root => ThrowIfDisposed(_root);
        internal IntTable Ints => ThrowIfDisposed(_ints);
        internal LongTable Longs => ThrowIfDisposed(_longs);
        internal BigIntTable BigIntegers => ThrowIfDisposed(_bigIntegers);
        internal FloatTable Floats => ThrowIfDisposed(_floats);
        internal DoubleTable Doubles => ThrowIfDisposed(_doubles);
        internal BigDecTable BigDecimals => ThrowIfDisposed(_bigDecimals);
        internal StringTable Strings => ThrowIfDisposed(_strings);
        internal ArrTable Arrs => ThrowIfDisposed(_arrs);
        internal ObjTable Objs => ThrowIfDisposed(_objs);

        static SickReader()
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new ConstraintException("big endian architecture is not supported");
            }
        }

        public bool IsDisposed => _disposed;

        public SickReader(
            ISickStream stream,
            ISickProfiler profiler,
            bool loadIndexes
        )
        {
            _stream = stream;
            _profiler = profiler;

            _header = ReadHeader(stream, profiler);
            _ints = new IntTable(_stream, _header.Offsets[0]);
            _longs = new LongTable(_stream, _header.Offsets[1]);
            _bigIntegers = new BigIntTable(_stream, _header.Offsets[2], loadIndexes);

            _floats = new FloatTable(_stream, _header.Offsets[3]);
            _doubles = new DoubleTable(_stream, _header.Offsets[4]);
            _bigDecimals = new BigDecTable(_stream, _header.Offsets[5], loadIndexes);

            _strings = new StringTable(_stream, _header.Offsets[6], loadIndexes);

            _arrs = new ArrTable(_stream, _header.Offsets[7], loadIndexes);
            _objs = new ObjTable(_stream, _strings, _header.Offsets[8], _header.Settings, loadIndexes);
            _root = new RootTable(_stream, _header.Offsets[9]);

            var roots = new Dictionary<string, SickRef>();
            for (var i = 0; i < _root.Count; i++)
            {
                var rootEntry = _root.Read(i);
                var rootId = _strings.Read(rootEntry.Key);
                var root = rootEntry.Reference;

                roots.Add(rootId, root);
            }

            _roots = roots;
        }

        /**
         * Open SICK format file for reading.
         * <param name="filePath">Full path to serialized SICK file.</param>
         * <param name="cacheManager">SICK cache manager to support files cache reuse.</param>
         * <param name="profiler">Sick profiler for queries and reads tracing.</param>
         * <param name="loadInMemoryThreshold">Threshold for in-memory SICK loading.</param>
         * <param name="cacheStream">Enable or disable file stream cache.</param>
         * <param name="cachePageSize">File stream cache size in bytes.</param>
         * <param name="cacheInternedIndexes">Intern table indexes instead of reading it when file stream cache is used.</param>
         * <param name="lockFileStreamAccess">Use file stream with locks for thread-safe operations. Potentially slow, consider using cached stream.</param>
         */
        public static SickReader OpenFile(
            string filePath,
            ISickCacheManager cacheManager,
            ISickProfiler profiler,
            long loadInMemoryThreshold = 65536,
            bool cacheStream = true,
            int cachePageSize = 4192,
            bool cacheInternedIndexes = true,
            bool lockFileStreamAccess = true
        )
        {
            var info = new FileInfo(filePath);
            var loadIntoMemory = info.Length <= loadInMemoryThreshold;

            ISickStream stream;
            bool loadIndexes;
            if (loadIntoMemory)
            {
                // stream = new MemoryStream(File.ReadAllBytes(path));
                // there were reports that ReadAllBytes might be broken on IL2CPP
                stream = new ISickStream.Buffer(ReadAllBytesSafe(filePath));
                loadIndexes = !cacheInternedIndexes;
            }
            else if (cacheStream)
            {
                stream = new ISickStream.Cached(cacheManager, filePath, cachePageSize, profiler);
                loadIndexes = !cacheInternedIndexes;
            }
            else
            {
                var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                stream = lockFileStreamAccess ? new ISickStream.DefaultWithLock(fileStream) : new ISickStream.Default(fileStream);
                loadIndexes = true;
            }

            return new SickReader(stream, profiler, loadIndexes);
        }

        /**
         * Open SICK format file for reading.
         * <param name="bytes">Byte array of SICK serialized data.</param>
         * <param name="profiler">Sick profiler for queries and reads tracing.</param>
         */
        public static SickReader Open(
            byte[] bytes,
            ISickProfiler profiler
        )
        {
            var stream = new ISickStream.Buffer(bytes);
            return new SickReader(stream, profiler, false);
        }

#if SICK_DEBUG_TRAVEL
        public static volatile int TotalLookups = 0;
        public static volatile int TotalTravel = 0;
#endif

        public void Dispose()
        {
            if (_disposed) return;
            lock (_locker)
            {
                if (_disposed) return;
                _disposed = true;
                _stream.Close();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new InvalidOperationException("Sick reader has been disposed.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T ThrowIfDisposed<T>(T value)
        {
            if (_disposed)
            {
                throw new InvalidOperationException("Sick reader has been disposed.");
            }

            return value;
        }

        private static Header ReadHeader(ISickStream stream, ISickProfiler profiler)
        {
#if SICK_PROFILE_READER
            using (var cp = profiler.OnInvoke("ReadHeader"))
#endif
            {
                var version = stream.ReadInt32BE(0);
                var tableCount = stream.ReadInt32BE(sizeof(int));

                var expectedVersion = 0;
                if (version != expectedVersion)
                {
                    throw new FormatException(
                        $"Structural failure: SICK version expected to be {expectedVersion}, got {version}"
                    );
                }

                var expectedTableCount = 10;
                if (tableCount != expectedTableCount)
                {
                    throw new FormatException(
                        $"Structural failure: SICK table count expected to be {expectedTableCount}, got {tableCount}, stream {stream}"
                    );
                }

                var tableOffsets = new List<int>();

                var offset = sizeof(int) * 2;
                foreach (var t in Enumerable.Range(0, tableCount))
                {
                    var next = stream.ReadInt32BE(offset);
                    if (t > 0 && next <= tableOffsets[t - 1])
                    {
                        throw new FormatException(
                            $"Structural failure: wrong SICK format, table offset {next} expected to be more than previous table offset {tableOffsets[t - 1]}"
                        );
                    }

                    tableOffsets.Add(next);
                    offset += sizeof(int);
                }

                var bucketCount = stream.ReadUInt16BE(offset);

                // Console.WriteLine($"Offsets: {String.Join(",", tables)}, b SICKuckets: {bucketCount}" );
                var header = new Header(version, tableCount, tableOffsets, new ObjIndexing(bucketCount, 0));
                return header;
            }
        }

        /**
         * Safe file buffer reader.
         * There were reports that System.IO.File.ReadAllBytes might be broken on IL2CPP.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] ReadAllBytesSafe(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var fileLength = fs.Length;
            if (fileLength > int.MaxValue) throw new IOException($"{filePath} is too large");

            var bytes = new byte[fileLength];
            fs.ReadUpTo(bytes, 0, (int)fileLength);

            return bytes;
        }
    }
}