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
        public sealed class Bool : SickJson
        {
            public readonly bool Value;

            internal Bool(SickReader reader, Ref reference) : base(reader, RefKind.Bit, reference)
            {
                Value = reference.Value == 1;
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.Root, T> onRoot)
            {
                return onBool(Value);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnBool(Value);
            }

            public override bool AsBool()
            {
                return Value;
            }
        }
    }
}