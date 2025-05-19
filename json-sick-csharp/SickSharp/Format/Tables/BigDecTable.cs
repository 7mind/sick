using System;
using System.Numerics;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public sealed record BigDecimal(BigInteger Unscaled, int Scale, int Precision, int Signum);

    public sealed class BigDecTable : VarTable<BigDecimal>
    {
        public BigDecTable(SpanStream stream, int offset, bool loadIndexes) : base(stream, offset, loadIndexes)
        {
        }

        protected override BigDecimal Convert(ReadOnlySpan<byte> bytes)
        {
            var signum = bytes[..sizeof(int)].ReadInt32BE();
            var precision = bytes[sizeof(int)..(sizeof(int) * 2 + 1)].ReadInt32BE();
            var scale = bytes[(sizeof(int) * 2)..(sizeof(int) * 3 + 1)].ReadInt32BE();
            return new BigDecimal(new BigInteger(bytes[sizeof(int)..]), scale, precision, signum);
        }
    }
}