#nullable enable
using System;
using System.Numerics;
using SickSharp.Format.Tables;

namespace SickSharp
{
    public abstract partial class SickCursor
    {
        public sealed class SByte : SickCursor
        {
            public readonly sbyte Value;

            internal SByte(SickReader reader, SickRef reference) : base(reader, SickKind.SByte, reference)
            {
                Value = (sbyte)reference.Value;
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickRoot, T> onRoot)
            {
                return onByte(Value);
            }

            public override T? Match<T>(SickCursorMatcher<T> matcher) where T : class
            {
                return matcher.OnByte(Value);
            }

            public override sbyte AsSByte()
            {
                return Value;
            }

            public override byte AsByte()
            {
                return (byte)Value;
            }

            public override short AsShort()
            {
                return Value;
            }

            public override int AsInt()
            {
                return Value;
            }

            public override long AsLong()
            {
                return Value;
            }

            public override float AsFloat()
            {
                return Value;
            }

            public override double AsDouble()
            {
                return Value;
            }
        }
    }
}