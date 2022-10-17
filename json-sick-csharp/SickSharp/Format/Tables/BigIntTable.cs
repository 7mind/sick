using System;
using System.IO;
using System.Numerics;

namespace SickSharp.Format.Tables
{
    public class BigIntTable : VarTable<BigInteger>
    {
        public BigIntTable(Stream stream, UInt32 offset) : base(stream, offset)
        {
        }

        protected override BigInteger Convert(byte[] bytes)
        {
            return new BigInteger(bytes);
        }
    }
}