using System;
using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public class IntTable : FixedTable<int>
    {
        public IntTable(Stream stream, int offset) : base(stream)
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