using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    internal class ShortTable : FixedTable<short>
    {
        public ShortTable(Stream stream, int offset) : base(stream, offset)
        {
        }

        protected override short ElementByteLength()
        {
            return sizeof(short);
        }

        protected override short Convert(byte[] bytes)
        {
            return bytes.ReadInt16();
        }
    }
}