#nullable enable
using System;
using System.Numerics;
using SickSharp.Format.Tables;

namespace SickSharp
{
    public abstract partial class SickJson
    {
        public sealed class Int : Lazy<int>
        {
            internal Int(SickReader reader, Ref reference) : base(reader, RefKind.Int, reference)
            {
            }

            protected override int Create()
            {
                return Reader.Ints.Read(Ref.Value);
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.Root, T> onRoot)
            {
                return onInt(Value);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
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

            public override double AsDouble()
            {
                return Value;
            }
        }
    }
}