using System;
using System.IO;
using System.Numerics;

namespace SickSharp.Format.Tables
{
    public class BigIntTable : VarTable<BigInteger>
    {
        public BigIntTable(Stream stream, int offset) : base(stream, offset)
        {
        }

        protected override BigInteger Convert(ReadOnlySpan<byte> bytes)
        {
            return new BigInteger(bytes);
        }
    }
}