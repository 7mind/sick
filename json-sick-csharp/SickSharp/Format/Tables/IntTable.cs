using System;
using SickSharp.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public sealed class IntTable : FixedTable<int>
    {
        public IntTable(ISickStream stream, int offset) : base(stream)
        {
            SetStart(offset);
            ReadStandardCount();
        }

        protected override short ElementByteLength()
        {
            return sizeof(int);
        }

        protected override int Convert(ReadOnlySpan<byte> bytes)
        {
            return bytes.ReadInt32BE();
        }
    }
}