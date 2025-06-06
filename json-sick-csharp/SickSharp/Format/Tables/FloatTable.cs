using System;
using SickSharp.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public sealed class FloatTable : FixedTable<float>
    {
        public FloatTable(ISickStream stream, int offset) : base(stream)
        {
            SetStart(offset);
            ReadStandardCount();
        }

        protected override short ElementByteLength()
        {
            return sizeof(float);
        }

        protected override float Convert(ReadOnlySpan<byte> bytes)
        {
            return bytes.ReadFloatBE();
        }
    }
}