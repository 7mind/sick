#nullable enable
using System;
using System.Numerics;
using SickSharp.Format.Tables;

namespace SickSharp
{
    public abstract partial class SickCursor
    {
        public sealed class Int : LazyCursor<int>
        {
            internal Int(SickReader reader, SickRef reference) : base(reader, SickKind.Int, reference)
            {
            }

            protected override int Create()
            {
                return Reader.Ints.Read(Ref.Value);
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickRoot, T> onRoot)
            {
                return onInt(Value);
            }

            public override T? Match<T>(SickCursorMatcher<T> matcher) where T : class
            {
                return matcher.OnInt(Value);
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