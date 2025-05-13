#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using SickSharp.Encoder;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public class ObjTable : BasicVarTable<OneObjTable>
    {
        private readonly StringTable _strings;
        private readonly ObjIndexing _settings;

        public ObjTable(Stream stream, StringTable strings, int offset, ObjIndexing settings) : base(stream, offset)
        {
            _strings = strings;
            _settings = settings;
        }

        protected override OneObjTable BasicRead(int absoluteStartOffset, int byteLen)
        {
            return new OneObjTable(Stream, _strings, absoluteStartOffset, _settings);
        }
    }

    public class KHash
    {
        public static Int64 Compute(string s)
        {
            Int32 a = 0x6BADBEEF;
            foreach (var b in Encoding.UTF8.GetBytes(s))
            {
                a ^= a << 13;
                a += (a ^ b) << 8;
            }

            return (long)(a) & 0xffffffffL;
        }
    }

    public class ObjIndexing
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

    public class OneObjTable : FixedTable<ObjEntry>
    {
        private readonly StringTable _strings;

        // public readonly ushort[]? BucketStartOffsets;
        // public readonly Dictionary<UInt32, ushort>? BucketEndOffsets;
        public bool UseIndex { get; }
        private byte[]? _rawIndex;
        private static int _intSize = Fixed.IntEncoder.BlobSize();

        public OneObjTable(Stream stream, StringTable strings, int offset, ObjIndexing settings) : base(stream)
        {
            _strings = strings;

            var indexHeader = stream.ReadSpan(offset, ObjIndexing.IndexMemberSize).ReadUInt16BE();
            // var indexHeader = rawIndex.ReadUInt16BE(0);

            if (indexHeader == ObjIndexing.NoIndex)
            {
                SetStart(offset + ObjIndexing.IndexMemberSize);
                ReadStandardCount();
                UseIndex = false;
            }
            else
            {
                var indexSize = settings.BucketCount * ObjIndexing.IndexMemberSize;
                _rawIndex = stream.ReadBytes(offset, indexSize + _intSize);

                SetStart(offset + indexSize);
                Count = BinaryPrimitives.ReadInt32BigEndian(new ReadOnlySpan<byte>(_rawIndex, indexSize, _intSize));

                UseIndex = true;
            }

            if (Count >= ObjIndexing.MaxIndex)
            {
                throw new FormatException(
                    $"Structural failure: object size is {Count} but max index is {ObjIndexing.MaxIndex}, object offset was {offset}"
                );
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort BucketValue(uint bucket)
        {
            Debug.Assert(UseIndex && _rawIndex != null);
            return _rawIndex.ReadUInt16BE(ObjIndexing.IndexMemberSize * bucket);
        }

        protected override short ElementByteLength()
        {
            return sizeof(byte) + 2 * sizeof(int);
        }

        protected override ObjEntry Convert(ReadOnlySpan<byte> bytes)
        {
            var keyval = bytes[..sizeof(int)].ReadInt32BE();
            var kind = (RefKind)bytes[sizeof(int)];
            var value = bytes[(sizeof(int) + 1)..(sizeof(int) * 2 + 1)].ReadInt32BE();
            return new ObjEntry(keyval, new Ref(kind, value));
        }

        public ReadOnlySpan<byte> ReadKeyOnly(int index, out string key)
        {
            var bytes = ReadBytes(index);
            var keyval = bytes[..sizeof(int)].ReadInt32BE();
            key = _strings.Read(keyval);
            return bytes;
        }

        public KeyValuePair<string, Ref> ReadKey(int index)
        {
            var obj = Read(index);
            return new KeyValuePair<string, Ref>(_strings.Read(obj.Key), obj.Value);
        }

        public IEnumerator<KeyValuePair<string, Ref>> GetEnumerator()
        {
            return Content().GetEnumerator();
        }

        public IEnumerable<KeyValuePair<string, Ref>> Content()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return ReadKey(i);
            }
        }
    }

    public record ObjEntry(int Key, Ref Value);
}