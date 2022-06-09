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


        public BasicVarTable(Stream stream, int offset)
        {
            Stream = stream;
            Count = Stream.ReadInt32(offset);

            _sizeOffset = offset + sizeof(int);
            _dataOffset = _sizeOffset + sizeof(int) * (Count + 1);
        }

        public int Count { get; }


        public TV Read(int index)
        {
            Debug.Assert(index < Count);
            var szTarget = _sizeOffset + index * sizeof(int);
            var szTargetNext = _sizeOffset + (index + 1) * sizeof(int);
            var relativeDataOffset = Stream.ReadInt32(szTarget);
            var endOffset = Stream.ReadInt32(szTargetNext);

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
            var bytes = Stream.ReadBuffer(absoluteStartOffset, byteLen);
            return Convert(bytes);
        }

        protected abstract TV Convert(byte[] bytes);
    }
}