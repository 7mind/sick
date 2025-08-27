using System;
using System.Numerics;

namespace SickSharp.Decoder
{
    public static class BigIntDecoder
    {
        public static BigInteger Decode(ReadOnlySpan<byte> bytes)
        {
            return new BigInteger(bytes, isBigEndian: true);
        }
    }
}