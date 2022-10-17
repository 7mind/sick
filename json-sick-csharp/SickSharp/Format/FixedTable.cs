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
        private readonly Stream _stream;
        private UInt32 _offset;


        public FixedTable(Stream stream, UInt32 offset)
        {
            _stream = stream;
            Offset = offset;
        }

        protected UInt32 Offset
        {
            get => _offset;
            set
            {
                Count = _stream.ReadInt32(value);
                _offset = value + sizeof(int);
            }
            
        }

        public int Count { get; protected set; }

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
            var target = Offset + index * ElementByteLength();
            return Convert(ReadBytes(target, ElementByteLength()));
        }

        public byte[] ReadBytes(long at, int size)
        {
            return _stream.ReadBuffer(at, size);
        }
        
        protected abstract short ElementByteLength();

        protected abstract TV Convert(byte[] bytes);

        public override string ToString()
        {
            return $"{{Table with {Count} elements}}";
        }
    }
}