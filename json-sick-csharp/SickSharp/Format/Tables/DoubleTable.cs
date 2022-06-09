using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public class DoubleTable : FixedTable<double>
    {
        public DoubleTable(Stream stream, int offset) : base(stream, offset)
        {
        }

        protected override short ElementByteLength()
        {
            return sizeof(double);
        }

        protected override double Convert(byte[] bytes)
        {
            return bytes.ReadDouble();
        }
    }
}