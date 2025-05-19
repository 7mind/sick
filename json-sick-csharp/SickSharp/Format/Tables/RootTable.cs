using System;
using SickSharp.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public sealed class RootTable : FixedTable<SickRoot>
    {
        public RootTable(ISickStream stream, int offset) : base(stream)
        {
            SetStart(offset);
            ReadStandardCount();
        }

        protected override short ElementByteLength()
        {
            return sizeof(int) + sizeof(byte) + sizeof(int);
        }

        protected override SickRoot Convert(ReadOnlySpan<byte> bytes)
        {
            var keyval = bytes[..sizeof(int)].ReadInt32BE();
            var kind = (SickKind?)bytes[sizeof(int)];

            var value = bytes[(sizeof(int) + 1)..(sizeof(int) * 2 + 1)].ReadInt32BE();
            return new SickRoot(keyval, new SickRef(kind.Value, value));
        }
    }
}