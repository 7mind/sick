using System;
using SickSharp.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public sealed class LongTable : FixedTable<long>
    {
        public LongTable(ISickStream stream, int offset) : base(stream)
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