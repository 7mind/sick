using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public class RootTable : FixedTable<Root>
    {
        public RootTable(Stream stream, int offset) : base(stream, offset)
        {
        }

        protected override short ElementByteLength()
        {
            return sizeof(int) + sizeof(byte) + sizeof(int);
        }

        protected override Root Convert(byte[] bytes)
        {
            var keyval = bytes[..sizeof(int)].ReadInt32();
            var kind = (RefKind?)bytes[sizeof(int)];

            var value = bytes[(sizeof(int) + 1)..(sizeof(int) * 2 + 1)].ReadInt32();
            return new Root(keyval, new Ref(kind.Value, value));
        }
    }

    public record Root(int Key, Ref Reference);
}