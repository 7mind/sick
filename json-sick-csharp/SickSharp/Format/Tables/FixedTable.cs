using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using SickSharp.Primitives;

namespace SickSharp.Format
{
    internal abstract class FixedTable<TV>
    {
        protected readonly SpanStream Stream;

        public FixedTable(SpanStream stream)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TV Read(int index)
        {
            return Convert(ReadSpan(index));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ReadOnlySpan<byte> ReadSpan(int index)
        {
            Debug.Assert(index < Count);
            var offset = DataOffset + index * ElementByteLength();
            return Stream.ReadSpan(offset, ElementByteLength());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ReadOnlySpan<byte> ReadSpan(int index, int offset, int count)
        {
            Debug.Assert(index < Count);
            var streamOffset = DataOffset + index * ElementByteLength() + offset;
            return Stream.ReadSpan(streamOffset, count);
        }

        protected abstract short ElementByteLength();

        protected abstract TV Convert(ReadOnlySpan<byte> bytes);

        public override string ToString()
        {
            return $"{{Table with {Count} elements}}";
        }
    }
}