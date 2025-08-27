using System;
using System.Numerics;
using SickSharp.Decoder;
using SickSharp.IO;

namespace SickSharp.Format.Tables
{
    public sealed record BigDecimal(BigInteger Unscaled, int Scale, int Precision, int Signum);

    public sealed class BigDecTable : VarTable<BigDecimal>
    {
        public BigDecTable(ISickStream stream, int offset, bool loadIndexes) : base(stream, offset, loadIndexes)
        {
        }

        protected override BigDecimal Convert(ReadOnlySpan<byte> bytes)
        {
            return BigDecimalDecoder.Decode(bytes);
        }
    }
}