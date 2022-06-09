using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public class FloatTable : FixedTable<float>
    {
        public FloatTable(Stream stream, int offset) : base(stream, offset)
        {
        }

        protected override short ElementByteLength()
        {
            return sizeof(float);
        }

        protected override float Convert(byte[] bytes)
        {
            return bytes.ReadFloat();
        }
    }
}