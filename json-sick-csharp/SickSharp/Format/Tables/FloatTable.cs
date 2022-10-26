using System;
using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public class FloatTable : FixedTable<float>
    {
        public FloatTable(Stream stream, UInt32 offset) : base(stream)
        {
            SetStart(offset);
            ReadStandardCount();
        }

        protected override short ElementByteLength()
        {
            return sizeof(float);
        }

        protected override float Convert(byte[] bytes)
        {
            return bytes.ReadFloatBE();
        }
    }
}