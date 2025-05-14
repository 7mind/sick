#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format
{
    public abstract class BasicVarTable<TV>
    {
        private readonly int _dataOffset;
        private readonly int _sizeOffset;
        private readonly byte[]? _index;

        protected readonly Stream Stream;

        public BasicVarTable(Stream stream, int offset, bool loadIndexes)
        {
            Stream = stream;
            Count = Stream.ReadInt32BE(offset);

            _sizeOffset = offset + sizeof(int);
            _dataOffset = _sizeOffset + sizeof(int) * (Count + 1);
            if (loadIndexes)
            {
                _index = Stream.ReadBytes(_sizeOffset, sizeof(int) * (Count + 1));
            }
        }

        public int Count { get; }


        public TV Read(int index)
        {
            Debug.Assert(index < Count);

            int relativeDataOffset;
            int endOffset;
            if (_index != null)
            {
                relativeDataOffset = _index.ReadInt32BE(index * sizeof(int));
                endOffset = _index.ReadInt32BE((index + 1) * sizeof(int));
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
        protected VarTable(Stream stream, int offset, bool loadIndexes) : base(stream, offset, loadIndexes)
        {
        }

        protected override TV BasicRead(int absoluteStartOffset, int byteLen)
        {
            return Convert(Stream.ReadSpan(absoluteStartOffset, byteLen));
        }

        protected abstract TV Convert(ReadOnlySpan<byte> bytes);
    }
}