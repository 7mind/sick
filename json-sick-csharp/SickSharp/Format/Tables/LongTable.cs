using System;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    internal sealed class LongTable : FixedTable<long>
    {
        public LongTable(SpanStream stream, int offset) : base(stream)
        {
            SetStart(offset);
            ReadStandardCount();
        }

        protected override short ElementByteLength()
        {
            return sizeof(long);
        }

        protected override long Convert(ReadOnlySpan<byte> bytes)
        {
            return bytes.ReadInt64BE();
        }
    }
}