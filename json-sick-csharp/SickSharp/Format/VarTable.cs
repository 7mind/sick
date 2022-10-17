using System;
using System.Diagnostics;
using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format
{
    public abstract class BasicVarTable<TV>
    {
        private readonly UInt32 _dataOffset;
        private readonly UInt32 _sizeOffset;
        protected readonly Stream Stream;


        public BasicVarTable(Stream stream, UInt32 offset)
        {
            Stream = stream;
            Count = (uint)Stream.ReadInt32(offset);

            _sizeOffset = offset + sizeof(int);
            _dataOffset = _sizeOffset + sizeof(int) * (Count + 1);
        }

        public UInt32 Count { get; }


        public TV Read(int index)
        {
            Debug.Assert(index < Count);
            var szTarget = _sizeOffset + index * sizeof(int);
            var szTargetNext = _sizeOffset + (index + 1) * sizeof(int);
            var relativeDataOffset = (UInt32)Stream.ReadInt32(szTarget);
            var endOffset = (UInt32)Stream.ReadInt32(szTargetNext);

            var absoluteStartOffset = _dataOffset + relativeDataOffset;
            var byteLen = endOffset - relativeDataOffset;
            return BasicRead(absoluteStartOffset, byteLen);
        }

        protected abstract TV BasicRead(UInt32 absoluteStartOffset, UInt32 byteLen);
    }

    public abstract class VarTable<TV> : BasicVarTable<TV>
    {
        protected VarTable(Stream stream, UInt32 offset) : base(stream, offset)
        {
        }

        protected override TV BasicRead(UInt32 absoluteStartOffset, UInt32 byteLen)
        {
            var bytes = Stream.ReadBuffer(absoluteStartOffset, (int)byteLen);
            return Convert(bytes);
        }

        protected abstract TV Convert(byte[] bytes);
    }
}