using System.IO;
using System.Numerics;

namespace SickSharp.Format.Tables
{
    internal class BigIntTable : VarTable<BigInteger>
    {
        public BigIntTable(Stream stream, int offset) : base(stream, offset)
        {
        }

        protected override BigInteger Convert(byte[] bytes)
        {
            return new BigInteger(bytes);
        }
    }
}