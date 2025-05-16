using System;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    internal sealed class DoubleTable : FixedTable<double>
    {
        public DoubleTable(SpanStream stream, int offset) : base(stream)
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