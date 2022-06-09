using System.Diagnostics;
using System.IO;

namespace SickSharp.Format.Tables
{
    public class ByteTable : FixedTable<byte>
    {
        public ByteTable(Stream stream, int offset) : base(stream, offset)
        {
        }

        protected override short ElementByteLength()
        {
            return sizeof(byte);
        }

        protected override byte Convert(byte[] bytes)
        {
            Debug.Assert(bytes.Length == 1);
            return bytes[0];
        }
    }
}