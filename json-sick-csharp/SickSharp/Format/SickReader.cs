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
    public sealed partial class SickReader : IDisposable
    {
        private readonly Dictionary<string, Ref> _roots = new();
        private readonly SpanStream _stream;
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
                throw new ConstraintException("big endian architecure is not supported");
            }
        }

        public SickReader(
            Stream stream,
            ISickProfiler profiler,
            bool loadIndexes
        )
        {
            _stream = SpanStream.Create(stream);
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

        public static SickReader OpenFile(
            string path,
            ISickCacheManager cacheManager,
            ISickProfiler profiler,
            long loadInMemoryThreshold = 65536,
            bool cacheStream = true,
            int cachePageSize = 4192,
            bool cacheInternedIndexes = true
        )
        {
            var info = new FileInfo(path);
            var loadIntoMemory = info.Length <= loadInMemoryThreshold;

            Stream stream;
            bool loadIndexes;
            if (loadIntoMemory)
            {
                // stream = new MemoryStream(File.ReadAllBytes(path));
                // there were reports that ReadAllBytes might be broken on IL2CPP
                var buf = ReadAllBytesSafe(path);
                stream = new MemoryStream(buf, 0, buf.Length, writable: false, publiclyVisible: true);
                loadIndexes = !cacheInternedIndexes;
            }
            else if (cacheStream)
            {
                stream = new NonAllocPageCachedStream(cacheManager.Provide(path, cachePageSize, profiler));
                loadIndexes = !cacheInternedIndexes;
            }
            else
            {
                stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
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
                _stream.Position = 0;

                var version = _stream.ReadInt32BE();
                var tableCount = _stream.ReadInt32BE();

                var expectedVersion = 0;
                if (version != expectedVersion)
                {
                    throw new FormatException(
                        $"Structural failure: SICK version expected to be {expectedVersion}, got {version}");
                }

                var expectedTableCount = 10;
                if (tableCount != expectedTableCount)
                {
                    throw new FormatException(
                        $"Structural failure: SICK table count expected to be {expectedTableCount}, got {tableCount}, stream {_stream}");
                }

                var tableOffsets = new List<int>();

                foreach (var t in Enumerable.Range(0, tableCount))
                {
                    var next = _stream.ReadInt32BE();
                    if (t > 0 && next <= tableOffsets[t - 1])
                    {
                        throw new FormatException(
                            $"Structural failure: wrong SICK format, table offset {next} expected to be more than previous table offset {tableOffsets[t - 1]}");
                    }

                    tableOffsets.Add(next);
                }

                var bucketCount = _stream.ReadUInt16BE();

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