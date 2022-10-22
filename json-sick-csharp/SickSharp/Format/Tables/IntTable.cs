using System;
using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public class IntTable : FixedTable<int>
    {
        public IntTable(Stream stream, UInt32 offset) : base(stream)
        {
            SetStart(offset);
            ReadStandardCount();
        }

        protected override short ElementByteLength()
        {
            return sizeof(int);
        }

        protected override int Convert(byte[] bytes)
        {
            return bytes.ReadInt32();
        }
    }
}