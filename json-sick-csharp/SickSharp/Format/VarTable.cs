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
        private readonly byte[] _index;
        private readonly bool _readIndexes;
        
        public BasicVarTable(Stream stream, UInt32 offset)
        {
            Stream = stream;
            Count = (uint)Stream.ReadInt32BE(offset);

            _readIndexes = SickReader.LoadIndexes;
            _sizeOffset = offset + sizeof(int);
            _dataOffset = _sizeOffset + sizeof(int) * (Count + 1);
            if (_readIndexes)
            {
                _index = Stream.ReadBytes(_sizeOffset, (int)(sizeof(int) * (Count + 1)));
            }

        }

        public UInt32 Count { get; }


        public TV Read(int index)
        {
            Debug.Assert(index < Count);

            UInt32 relativeDataOffset;
            UInt32 endOffset;
            if (_readIndexes)
            {
                 relativeDataOffset =
                    (UInt32)_index.ReadInt32BE((uint)index * sizeof(int));
                 endOffset = (UInt32)_index.ReadInt32BE((uint)(index + 1) * sizeof(int));
            }
            else
            {
                var szTarget = _sizeOffset + index * sizeof(int);
                var szTargetNext = _sizeOffset + (index + 1) * sizeof(int);
                relativeDataOffset = (UInt32)Stream.ReadInt32BE(szTarget);
                endOffset = (UInt32)Stream.ReadInt32BE(szTargetNext);
            }

            
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