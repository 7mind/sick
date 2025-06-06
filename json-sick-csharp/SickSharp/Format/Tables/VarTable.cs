#nullable enable
using System;
using System.Diagnostics;
using SickSharp.IO;
using SickSharp.Primitives;

namespace SickSharp.Format
{
    public abstract class BasicVarTable<TV>
    {
        private readonly int _dataOffset;
        private readonly int _sizeOffset;
        private readonly ReadOnlyMemory<byte>? _index;

        protected readonly ISickStream Stream;

        protected BasicVarTable(ISickStream stream, int offset, bool loadIndexes)
        {
            Stream = stream;
            Count = Stream.ReadInt32BE(offset);

            _sizeOffset = offset + sizeof(int);
            _dataOffset = _sizeOffset + sizeof(int) * (Count + 1);
            _index = loadIndexes ? Stream.ReadMemory(_sizeOffset, sizeof(int) * (Count + 1)) : (ReadOnlyMemory<byte>?)null;
        }

        public int Count { get; }

        internal TV Read(int index)
        {
            Debug.Assert(index < Count);

            int relativeDataOffset;
            int endOffset;
            if (_index.HasValue)
            {
                relativeDataOffset = _index.Value.Slice(index * sizeof(int), sizeof(int)).Span.ReadInt32BE();
                endOffset = _index.Value.Slice((index + 1) * sizeof(int), sizeof(int)).Span.ReadInt32BE();
            }
            else
            {
                var szTarget = _sizeOffset + index * sizeof(int);
                var szTargetNext = szTarget + sizeof(int);
                relativeDataOffset = Stream.ReadInt32BE(szTarget);
                endOffset = Stream.ReadInt32BE(szTargetNext);
            }

            var absoluteStartOffset = _dataOffset + relativeDataOffset;
            var byteLen = endOffset - relativeDataOffset;
            return BasicRead(absoluteStartOffset, byteLen);
        }

        protected abstract TV BasicRead(int absoluteStartOffset, int byteLen);
    }

    public abstract class VarTable<TV> : BasicVarTable<TV>
    {
        protected VarTable(ISickStream stream, int offset, bool loadIndexes) : base(stream, offset, loadIndexes)
        {
        }

        protected override TV BasicRead(int absoluteStartOffset, int byteLen)
        {
            return Convert(Stream.ReadSpan(absoluteStartOffset, byteLen));
        }

        protected abstract TV Convert(ReadOnlySpan<byte> bytes);
    }
}