#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using SickSharp.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    internal sealed class ObjTable : BasicVarTable<OneObjTable>
    {
        private readonly StringTable _strings;
        private readonly ObjIndexing _settings;
        private readonly bool _loadIndexes;

        public ObjTable(ISickStream stream, StringTable strings, int offset, ObjIndexing settings, bool loadIndexes) : base(stream, offset, loadIndexes)
        {
            _strings = strings;
            _settings = settings;
            _loadIndexes = loadIndexes;
        }

        protected override OneObjTable BasicRead(int absoluteStartOffset, int byteLen)
        {
            return new OneObjTable(Stream, _strings, absoluteStartOffset, _settings, _loadIndexes);
        }
    }

    internal static class KHash
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Compute(string s)
        {
            Int32 a = 0x6BADBEEF;
            foreach (var b in Encoding.UTF8.GetBytes(s))
            {
                a ^= a << 13;
                a += (a ^ b) << 8;
            }

            return (long)(a) & 0xffffffffL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (long Hash, int Bucket) Bucket(string str, long bucketSize)
        {
            var hash = Compute(str);
            var bucket = Convert.ToInt32(hash / bucketSize);
            Debug.Assert(bucket < bucketSize && bucket >= 0);
            return (hash, bucket);
        }
    }

    public record ObjEntry(int Key, SickRef Value);

    public sealed class ObjIndexing
    {
        public const ushort NoIndex = 65535;

        public const ushort MaxIndex = NoIndex - 1;

        //public const ushort BucketCount = 16;
        public const long Range = (long)UInt32.MaxValue + 1;
        public readonly ushort BucketCount;
        public readonly long BucketSize;
        public readonly ushort Limit;


        public ObjIndexing(ushort bucketCount, ushort limit)
        {
            BucketCount = bucketCount;
            Limit = limit;
            BucketSize = Range / BucketCount;
            Debug.Assert(Range == 4294967296);
            Debug.Assert(BucketCount > 1);
            Debug.Assert(Range % BucketCount == 0);
        }

        public const ushort IndexMemberSize = sizeof(ushort);
    }

    public sealed class OneObjTable : FixedTable<ObjEntry>
    {
        private readonly StringTable _strings;
        private readonly int _offset;
        private readonly ReadOnlyMemory<byte>? _index;

        public bool UseIndex { get; }

        public OneObjTable(ISickStream stream, StringTable strings, int offset, ObjIndexing settings, bool loadIndexes) : base(stream)
        {
            _strings = strings;
            _offset = offset;

            var indexHeader = stream.ReadSpan(offset, ObjIndexing.IndexMemberSize).ReadUInt16BE();
            UseIndex = indexHeader != ObjIndexing.NoIndex;

            if (UseIndex)
            {
                var indexSize = settings.BucketCount * ObjIndexing.IndexMemberSize;
                SetStart(offset + indexSize);

                if (loadIndexes)
                {
                    _index = Stream.ReadMemory(offset, indexSize + sizeof(int));
                    Count = _index.Value.Slice(indexSize, sizeof(int)).Span.ReadInt32BE();
                }
                else
                {
                    Count = Stream.ReadInt32BE(_offset + indexSize);
                }
            }
            else
            {
                SetStart(offset + ObjIndexing.IndexMemberSize);
                ReadStandardCount();
            }

            if (Count >= ObjIndexing.MaxIndex)
            {
                throw new FormatException(
                    $"Structural failure: object size is {Count} but max index is {ObjIndexing.MaxIndex}, object offset was {offset}"
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort BucketValue(int bucket)
        {
            Debug.Assert(UseIndex);
            var start = ObjIndexing.IndexMemberSize * bucket;
            return _index.HasValue ? _index.Value.Slice(start, sizeof(ushort)).Span.ReadUInt16BE() : Stream.ReadUInt16BE(_offset + start);
        }

        protected override short ElementByteLength()
        {
            return sizeof(byte) + 2 * sizeof(int);
        }

        protected override ObjEntry Convert(ReadOnlySpan<byte> bytes)
        {
            var keyval = bytes.Slice(0, sizeof(int)).ReadInt32BE();
            var kind = (SickKind)bytes[sizeof(int)];
            var value = bytes.Slice(sizeof(int) + sizeof(byte), sizeof(int)).ReadInt32BE();
            return new ObjEntry(keyval, new SickRef(kind, value));
        }

        public KeyValuePair<string, SickRef> ReadKey(int index)
        {
            var obj = Read(index);
            return new KeyValuePair<string, SickRef>(_strings.Read(obj.Key), obj.Value);
        }

        public SickRef ReadRef(int index)
        {
            var bytes = ReadSpan(index, sizeof(int), sizeof(byte) + sizeof(int));
            return ConvertRef(bytes);
        }

        public ReadOnlySpan<byte> ReadKeyRefSpan(int index, out string key)
        {
            var bytes = ReadSpan(index);
            var keyval = bytes[..sizeof(int)].ReadInt32BE();
            key = _strings.Read(keyval);
            return bytes[sizeof(int)..];
        }

        public IEnumerator<KeyValuePair<string, SickRef>> GetEnumerator()
        {
            return Content().GetEnumerator();
        }

        public IEnumerable<KeyValuePair<string, SickRef>> Content()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return ReadKey(i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SickRef ConvertRef(ReadOnlySpan<byte> bytes)
        {
            var kind = (SickKind)bytes[0];
            var value = bytes.Slice(sizeof(byte), sizeof(int)).ReadInt32BE();
            return new SickRef(kind, value);
        }
    }
}