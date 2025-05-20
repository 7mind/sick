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
        public sealed class Long : LazySickJson<long>
        {
            internal Long(SickReader reader, SickRef reference) : base(reader, SickKind.Long, reference)
            {
            }

            protected override long Create()
            {
                return Reader.Longs.Read(Ref.Value);
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.SickRoot, T> onRoot)
            {
                return onLong(Value);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnLong(Value);
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