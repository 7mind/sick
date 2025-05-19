using System;
using SickSharp.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public sealed class DoubleTable : FixedTable<double>
    {
        public DoubleTable(ISickStream stream, int offset) : base(stream)
        {
            SetStart(offset);
            ReadStandardCount();
        }

        protected override short ElementByteLength()
        {
            return sizeof(double);
        }

        protected override double Convert(ReadOnlySpan<byte> bytes)
        {
            return bytes.ReadDoubleBE();
        }
    }
}