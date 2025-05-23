#nullable enable
using System;
using System.Numerics;
using SickSharp.Format.Tables;

namespace SickSharp
{
    public abstract partial class SickCursor
    {
        public sealed class BigDec : LazyCursor<BigDecimal>
        {
            internal BigDec(SickReader reader, SickRef reference) : base(reader, SickKind.BigDec, reference)
            {
            }

            protected override BigDecimal Create()
            {
                return Reader.BigDecimals.Read(Ref.Value);
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickRoot, T> onRoot)
            {
                return onBigDecimal(Value);
            }

            public override T? Match<T>(SickCursorMatcher<T> matcher) where T : class
            {
                return matcher.OnBigDecimal(Value);
            }
        }
    }
}