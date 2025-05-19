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
        private readonly Dictionary<string, SickRef> _roots = new();
        private readonly ISickStream _stream;
        internal readonly ISickProfiler Profiler;
        internal readonly Header Header;
        internal readonly RootTable Root;
        internal readonly IntTable Ints;
        internal readonly LongTable Longs;
        internal readonly BigIntTable BigIntegers;
        internal readonly FloatTable Floats;
        internal readonly DoubleTable Doubles;
        internal readonly BigDecTable BigDecimals;
        internal readonly StringTable Strings;
        internal readonly ArrTable Arrs;
        internal readonly ObjTable Objs;

        static SickReader()
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new ConstraintException("big endian architecture is not supported");
            }
        }

        public SickReader(
            ISickStream stream,
            ISickProfiler profiler,
            bool loadIndexes
        )
        {
            _stream = stream;
            Profiler = profiler;
            Header = ReadHeader();

            Ints = new IntTable(_stream, Header.Offsets[0]);
            Longs = new LongTable(_stream, Header.Offsets[1]);
            BigIntegers = new BigIntTable(_stream, Header.Offsets[2], loadIndexes);

            Floats = new FloatTable(_stream, Header.Offsets[3]);
            Doubles = new DoubleTable(_stream, Header.Offsets[4]);
            BigDecimals = new BigDecTable(_stream, Header.Offsets[5], loadIndexes);

            Strings = new StringTable(_stream, Header.Offsets[6], loadIndexes);

            Arrs = new ArrTable(_stream, Header.Offsets[7], loadIndexes);
            Objs = new ObjTable(_stream, Strings, Header.Offsets[8], Header.Settings, loadIndexes);
            Root = new RootTable(_stream, Header.Offsets[9]);

            for (var i = 0; i < Root.Count; i++)
            {
                var rootEntry = Root.Read(i);
                var rootId = Strings.Read(rootEntry.Key);
                var root = rootEntry.Reference;

                _roots.Add(rootId, root);
            }
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

#if SICK_DEBUG_TRAVEL
        public static volatile int TotalLookups = 0;
        public static volatile int TotalTravel = 0;
#endif

        public void Dispose()
        {
            _stream.Close();
        }

        private Header ReadHeader()
        {
#if SICK_PROFILE_READER
            using (var cp = Profiler.OnInvoke("ReadHeader"))
#endif
            {
                var version = _stream.ReadInt32BE(0);
                var tableCount = _stream.ReadInt32BE(sizeof(int));

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
                        $"Structural failure: SICK table count expected to be {expectedTableCount}, got {tableCount}, stream {_stream}"
                    );
                }

                var tableOffsets = new List<int>();

                var offset = sizeof(int) * 2;
                foreach (var t in Enumerable.Range(0, tableCount))
                {
                    var next = _stream.ReadInt32BE(offset);
                    if (t > 0 && next <= tableOffsets[t - 1])
                    {
                        throw new FormatException(
                            $"Structural failure: wrong SICK format, table offset {next} expected to be more than previous table offset {tableOffsets[t - 1]}"
                        );
                    }

                    tableOffsets.Add(next);
                    offset += sizeof(int);
                }

                var bucketCount = _stream.ReadUInt16BE(offset);

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