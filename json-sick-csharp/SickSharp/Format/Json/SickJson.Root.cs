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
        public sealed class Root : Lazy<SickSharp.Root>
        {
            internal Root(SickReader reader, Ref reference) : base(reader, RefKind.Root, reference)
            {
            }

            protected override SickSharp.Root Create()
            {
                return Reader.Root.Read(Ref.Value);
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.Root, T> onRoot)
            {
                return onRoot(Value);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnRoot(Value);
            }
        }
    }
}