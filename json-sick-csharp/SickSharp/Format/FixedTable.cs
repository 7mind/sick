using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format
{
    public abstract class FixedTable<TV>
    {
        //private readonly int _offset;
        protected readonly Stream Stream;
        // private UInt32 _offset;


        public FixedTable(Stream stream)
        {
            Stream = stream;
        }

        public int Count { get; protected set; }
        public int StartOffset { get; protected set; }
        public int DataOffset { get; protected set; }

        protected void SetStart(int offset)
        {
            StartOffset = offset;
            DataOffset = offset + sizeof(int);
        }

        protected void ReadStandardCount()
        {
            Count = Stream.ReadInt32BE(StartOffset);
        }

        // Call this carefully, it may explode!
        // Only use it on small collections and only when you are completely sure that they are actually small
        public List<TV> ReadAll()
        {
            var result = new List<TV>();
            for (var i = 0; i < Count; i++) result.Add(Read(i));
            return result;
        }

        public ReadOnlySpan<byte> ReadSpanOfEntity(int index)
        {
            Debug.Assert(index < Count);
            var offset = DataOffset + index * ElementByteLength();
            return Stream.ReadSpan(offset, ElementByteLength());
        }

        public TV Read(int index)
        {
            return Convert(ReadSpanOfEntity(index));
        }

        protected abstract short ElementByteLength();

        protected abstract TV Convert(ReadOnlySpan<byte> bytes);

        public override string ToString()
        {
            return $"{{Table with {Count} elements}}";
        }
    }
}