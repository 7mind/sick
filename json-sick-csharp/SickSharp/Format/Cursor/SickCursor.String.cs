#nullable enable
using System;
using System.Numerics;
using SickSharp.Format.Tables;

namespace SickSharp
{
    public abstract partial class SickCursor
    {
        public sealed class String : LazyCursor<string>
        {
            internal String(SickReader reader, SickRef reference) : base(reader, SickKind.String, reference)
            {
            }

            protected override string Create()
            {
                return Reader.Strings.Read(Ref.Value);
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickRoot, T> onRoot)
            {
                return onString(Value);
            }

            public override T? Match<T>(SickCursorMatcher<T> matcher) where T : class
            {
                return matcher.OnString(Value);
            }

            public override string AsString()
            {
                return Value;
            }
        }
    }
}