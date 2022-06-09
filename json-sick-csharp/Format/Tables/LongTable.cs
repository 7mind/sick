using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    internal class LongTable : FixedTable<long>
    {
        public LongTable(Stream stream, int offset) : base(stream, offset)
        {
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