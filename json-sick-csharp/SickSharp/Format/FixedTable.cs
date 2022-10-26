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
        // private UInt32 _offset;


        public FixedTable(Stream stream)
        {
            _stream = stream;

        }

        public int Count { get; protected set; }
        public UInt32 StartOffset { get; protected set; }
        public UInt32 DataOffset { get; protected set; }

        protected void SetStart(UInt32 offset)
        {
            StartOffset = offset;
            DataOffset = offset +  sizeof(int);
        }
        
        protected void ReadStandardCount()
        {
            Count = _stream.ReadInt32BE(StartOffset);
        }
        
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
            var target = DataOffset + index * ElementByteLength();
            return Convert(_stream.ReadBytes(target, ElementByteLength()));
        }
        
        protected abstract short ElementByteLength();

        protected abstract TV Convert(byte[] bytes);

        public override string ToString()
        {
            return $"{{Table with {Count} elements}}";
        }
    }
}