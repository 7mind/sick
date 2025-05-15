using System;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public class RootTable : FixedTable<Root>
    {
        public RootTable(SpanStream stream, int offset) : base(stream)
        {
            SetStart(offset);
            ReadStandardCount();
        }

        protected override short ElementByteLength()
        {
            return sizeof(int) + sizeof(byte) + sizeof(int);
        }

        protected override Root Convert(ReadOnlySpan<byte> bytes)
        {
            var keyval = bytes[..sizeof(int)].ReadInt32BE();
            var kind = (RefKind?)bytes[sizeof(int)];

            var value = bytes[(sizeof(int) + 1)..(sizeof(int) * 2 + 1)].ReadInt32BE();
            return new Root(keyval, new Ref(kind.Value, value));
        }
    }

    public record Root(int Key, Ref Reference);
}