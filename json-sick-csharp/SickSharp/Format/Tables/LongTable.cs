using System;
using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public class LongTable : FixedTable<long>
    {
        public LongTable(Stream stream, UInt32 offset) : base(stream)
        {
            SetStart(offset);
            ReadStandardCount();
        }

        protected override short ElementByteLength()
        {
            return sizeof(long);
        }

        protected override long Convert(byte[] bytes)
        {
            return bytes.ReadInt64();
        }
    }
}