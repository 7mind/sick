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
        private readonly ISickProfiler _profiler;
        private readonly Header _header;
        private readonly RootTable _root;
        private readonly IntTable _ints;
        private readonly LongTable _longs;
        private readonly BigIntTable _bigInts;
        private readonly FloatTable _floats;
        private readonly DoubleTable _doubles;
        private readonly BigDecTable _bigDecimals;
        private readonly StringTable _strings;
        private readonly ArrTable _arrs;
        private readonly ObjTable _objs;

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
            _profiler = profiler;
            _header = ReadHeader();

            _ints = new IntTable(_stream, _header.Offsets[0]);
            _longs = new LongTable(_stream, _header.Offsets[1]);
            _bigInts = new BigIntTable(_stream, _header.Offsets[2], loadIndexes);

            _floats = new FloatTable(_stream, _header.Offsets[3]);
            _doubles = new DoubleTable(_stream, _header.Offsets[4]);
            _bigDecimals = new BigDecTable(_stream, _header.Offsets[5], loadIndexes);

            _strings = new StringTable(_stream, _header.Offsets[6], loadIndexes);

            _arrs = new ArrTable(_stream, _header.Offsets[7], loadIndexes);
            _objs = new ObjTable(_stream, _strings, _header.Offsets[8], _header.Settings, loadIndexes);
            _root = new RootTable(_stream, _header.Offsets[9]);

            for (var i = 0; i < _root.Count; i++)
            {
                var rootEntry = _root.Read(i);
                var rootId = _strings.Read(rootEntry.Key);
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
                var buf = ReadAllBytes2(path);
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

        private static byte[] ReadAllBytes2(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long fileLength = fs.Length;
                if (fileLength > int.MaxValue) throw new IOException($"{filePath} is too large");

                byte[] bytes = new byte[fileLength];

                fs.ReadUpTo(bytes, 0, (int)fileLength);

                return bytes;
            }
        }

#if SICK_DEBUG_TRAVEL
        public static volatile int TotalLookups = 0;
        public static volatile int TotalTravel = 0;
#endif

        public Ref? GetRoot(string id)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("GetRoot", id))
#endif
            {
                var ret = _roots.GetValueOrDefault(id);
#if SICK_PROFILE_READER
                return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }

        public void Dispose()
        {
            _stream.Close();
        }

        private Header ReadHeader()
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("ReadHeader"))
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<string> HandleBracketsWithoutDot(ref string currentQuery, ReadOnlySpan<string> current)
        {
            var span = current.Slice(1);

            var maybeIndex = ExtractIndex(ref currentQuery);
            if (maybeIndex != null)
            {
                var newArray = new string[span.Length + 1];
                newArray[0] = maybeIndex;
                span.CopyTo(newArray.AsSpan(1));
                span = newArray;
            }

            return span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string? ExtractIndex(ref string currentQuery)
        {
            var indexStart = currentQuery.IndexOf('[');
            // we have [ but not as the first symbol
            if (indexStart > 0 && currentQuery.EndsWith(']'))
            {
                var index = currentQuery.Substring(indexStart);
                currentQuery = currentQuery.Substring(0, indexStart);
                return index;
            }

            return null;
        }
    }
}