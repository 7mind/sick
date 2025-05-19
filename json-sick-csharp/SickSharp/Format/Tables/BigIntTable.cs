using System;
using System.Numerics;
using SickSharp.IO;

namespace SickSharp.Format.Tables
{
    public sealed class BigIntTable : VarTable<BigInteger>
    {
        public BigIntTable(ISickStream stream, int offset, bool loadIndexes) : base(stream, offset, loadIndexes)
        {
        }

        protected override BigInteger Convert(ReadOnlySpan<byte> bytes)
        {
            return new BigInteger(bytes);
        }
    }
}