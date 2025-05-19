#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SickSharp.Format.Tables;

namespace SickSharp
{
    public abstract partial class SickJson
    {
        public sealed class Float : Lazy<float>
        {
            internal Float(SickReader reader, Ref reference) : base(reader, RefKind.Float, reference)
            {
            }

            protected override float Create()
            {
                return Reader.Floats.Read(Ref.Value);
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.Root, T> onRoot)
            {
                return onFloat(Value);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnFloat(Value);
            }

            public override double AsDouble()
            {
                return Value;
            }
        }
    }
}