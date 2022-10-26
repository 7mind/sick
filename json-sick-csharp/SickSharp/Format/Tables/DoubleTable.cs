using System;
using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public class DoubleTable : FixedTable<double>
    {
        public DoubleTable(Stream stream, UInt32 offset) : base(stream)
        {
            SetStart(offset);
            ReadStandardCount();
        }

        protected override short ElementByteLength()
        {
            return sizeof(double);
        }

        protected override double Convert(byte[] bytes)
        {
            return bytes.ReadDoubleBE();
        }
    }
}