using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    internal class IntTable : FixedTable<int>
    {
        public IntTable(Stream stream, int offset) : base(stream, offset)
        {
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