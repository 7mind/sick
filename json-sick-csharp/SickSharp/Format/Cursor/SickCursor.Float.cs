#nullable enable
using System;
using System.Numerics;
using SickSharp.Format.Tables;

namespace SickSharp
{
    public abstract partial class SickCursor
    {
        public sealed class Float : LazyCursor<float>
        {
            internal Float(SickReader reader, SickRef reference) : base(reader, SickKind.Float, reference)
            {
            }

            protected override float Create()
            {
                return Reader.Floats.Read(Ref.Value);
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickRoot, T> onRoot)
            {
                return onFloat(Value);
            }

            public override T? Match<T>(SickCursorMatcher<T> matcher) where T : class
            {
                return matcher.OnFloat(Value);
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