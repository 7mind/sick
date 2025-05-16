using System;
using System.Numerics;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    internal sealed class BigIntTable : VarTable<BigInteger>
    {
        public BigIntTable(SpanStream stream, int offset, bool loadIndexes) : base(stream, offset, loadIndexes)
        {
        }

        protected override BigInteger Convert(ReadOnlySpan<byte> bytes)
        {
            return new BigInteger(bytes);
        }
    }
}