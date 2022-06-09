using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format
{
    public abstract class FixedTable<TV>
    {
        private readonly int _offset;
        private readonly Stream _stream;


        public FixedTable(Stream stream, int offset)
        {
            _stream = stream;
            Count = _stream.ReadInt32(offset);
            _offset = offset + sizeof(int);
        }

        public int Count { get; }

        // Call this carefully, it may explode!
        // Only use it on small collections and only when you are completely sure that they are actually small
        public List<TV> ReadAll()
        {
            var result = new List<TV>();
            for (var i = 0; i < Count; i++) result.Add(Read(i));
            return result;
        }

        public TV Read(int index)
        {
            Debug.Assert(index < Count);
            var target = _offset + index * ElementByteLength();
            var bytes = _stream.ReadBuffer(target, ElementByteLength());
            return Convert(bytes);
        }

        protected abstract short ElementByteLength();

        protected abstract TV Convert(byte[] bytes);

        public override string ToString()
        {
            return $"{{Table with {Count} elements}}";
        }
    }
}