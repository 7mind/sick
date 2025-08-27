using System;
using System.Numerics;
using SickSharp.Format.Tables;
using SickSharp.Primitives;

namespace SickSharp.Decoder
{
    public static class BigDecimalDecoder
    {
        public static BigDecimal Decode(ReadOnlySpan<byte> bytes)
        {
            var signum = bytes[..sizeof(int)].ReadInt32BE();
            var precision = bytes[sizeof(int)..(sizeof(int) * 2)].ReadInt32BE();
            var scale = bytes[(sizeof(int) * 2)..(sizeof(int) * 3)].ReadInt32BE();
            var unscaledBytes = bytes[(sizeof(int) * 3)..];
            return new BigDecimal(new BigInteger(unscaledBytes, isBigEndian: true), scale, precision, signum);
        }
    }
}
