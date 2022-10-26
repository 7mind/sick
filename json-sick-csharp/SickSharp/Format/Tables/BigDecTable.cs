using System;
using System.IO;
using System.Linq;
using System.Numerics;
using SickSharp.Primitives;


namespace SickSharp.Format.Tables
{
    public record BigDecimal(BigInteger Unscaled, int Scale, int Precision, int Signum);

    public class BigDecTable : VarTable<BigDecimal>
    {
        public BigDecTable(Stream stream, UInt32 offset) : base(stream, offset)
        {
        }

        protected override BigDecimal Convert(byte[] bytes)
        {
            var signum = bytes[..sizeof(int)].ReadInt32BE();
            var precision = bytes[sizeof(int)..(sizeof(int) * 2 + 1)].ReadInt32BE();
            var scale = bytes[(sizeof(int) * 2)..(sizeof(int) * 3 + 1)].ReadInt32BE();
            return new BigDecimal(new BigInteger(bytes.Skip(sizeof(int)).ToArray()), scale, precision, signum);
        }
    }
}