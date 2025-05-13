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
        protected readonly Stream Stream;
        private readonly byte[] _index;
        private readonly bool _readIndexes;

        public BasicVarTable(Stream stream, int offset)
        {
            Stream = stream;
            Count = Stream.ReadInt32BE(offset);

            _readIndexes = SickReader.LoadIndexes;
            _sizeOffset = offset + sizeof(int);
            _dataOffset = _sizeOffset + sizeof(int) * (Count + 1);
            if (_readIndexes)
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
            if (_readIndexes)
            {
                relativeDataOffset = _index.ReadInt32BE((uint)index * sizeof(int));
                endOffset = _index.ReadInt32BE((uint)(index + 1) * sizeof(int));
            }
            else
            {
                var szTarget = _sizeOffset + index * sizeof(int);
                var szTargetNext = _sizeOffset + (index + 1) * sizeof(int);
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
        protected VarTable(Stream stream, int offset) : base(stream, offset)
        {
        }

        protected override TV BasicRead(int absoluteStartOffset, int byteLen)
        {
            return Convert(Stream.ReadSpan(absoluteStartOffset, byteLen));
        }

        protected abstract TV Convert(ReadOnlySpan<byte> bytes);
    }
}