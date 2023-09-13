#nullable enable
using System;
using System.Numerics;

namespace SickSharp.Format.Tables
{
    public class JsonMatcher<T> where T : class
    {
        public virtual T? OnNull()
        {
            return null;
        }

        public virtual T? OnBool(bool value)
        {
            return null;
        }

        public virtual T? OnByte(sbyte value)
        {
            return null;
        }

        public virtual T? OnShort(short value)
        {
            return null;
        }

        public virtual T? OnInt(int value)
        {
            return null;
        }

        public virtual T? OnLong(long value)
        {
            return null;
        }

        public virtual T? OnBigInt(BigInteger value)
        {
            return null;
        }

        public virtual T? OnFloat(float value)
        {
            return null;
        }

        public virtual T? OnDouble(double value)
        {
            return null;
        }

        public virtual T? OnBigDecimal(BigDecimal value)
        {
            return null;
        }

        public virtual T? OnString(string value)
        {
            return null;
        }

        public virtual T? OnArray(OneArrTable value)
        {
            return null;
        }

        public virtual T? OnObj(OneObjTable value)
        {
            return null;
        }

        public virtual T? OnRoot(Root value)
        {
            return null;
        }
    }

    public interface IJsonVal
    {
        public T Match<T>(
            Func<T> onNull,
            Func<bool, T> onBool,
            Func<sbyte, T> onByte,
            Func<short, T> onShort,
            Func<int, T> onInt,
            Func<long, T> onLong,
            Func<BigInteger, T> onBigInt,
            Func<float, T> onFloat,
            Func<double, T> onDouble,
            Func<BigDecimal, T> onBigDecimal,
            Func<string, T> onString,
            Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj,
            Func<Root, T> onRoot
        );

        public T? Match<T>(JsonMatcher<T> matcher) where T : class;
    }

    public sealed record JNull : IJsonVal
    {
        public T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
            Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
            Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj, Func<Root, T> onRoot)
        {
            return onNull();
        }

        public T? Match<T>(JsonMatcher<T> matcher) where T : class
        {
            return matcher.OnNull();
        }
    }

    public sealed record JBool(bool Value) : IJsonVal
    {
        public T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
            Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
            Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj, Func<Root, T> onRoot)
        {
            return onBool(Value);
        }

        public T? Match<T>(JsonMatcher<T> matcher) where T : class
        {
            return matcher.OnBool(Value);
        }
    }

    public sealed record JSByte(sbyte Value) : IJsonVal
    {
        public T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
            Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
            Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj, Func<Root, T> onRoot)
        {
            return onByte(Value);
        }

        public T? Match<T>(JsonMatcher<T> matcher) where T : class
        {
            return matcher.OnByte(Value);
        }
    }

    public sealed record JShort(short Value) : IJsonVal
    {
        public T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
            Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
            Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj, Func<Root, T> onRoot)
        {
            return onShort(Value);
        }

        public T? Match<T>(JsonMatcher<T> matcher) where T : class
        {
            return matcher.OnShort(Value);
        }
    }

    public sealed record JInt(int Value) : IJsonVal
    {
        public T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
            Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
            Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj, Func<Root, T> onRoot)
        {
            return onInt(Value);
        }

        public T? Match<T>(JsonMatcher<T> matcher) where T : class
        {
            return matcher.OnInt(Value);
        }
    }

    public sealed record JLong(long Value) : IJsonVal
    {
        public T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
            Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
            Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj, Func<Root, T> onRoot)
        {
            return onLong(Value);
        }

        public T? Match<T>(JsonMatcher<T> matcher) where T : class
        {
            return matcher.OnLong(Value);
        }
    }

    public sealed record JBigInt(BigInteger Value) : IJsonVal
    {
        public T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
            Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
            Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj, Func<Root, T> onRoot)
        {
            return onBigInt(Value);
        }

        public T? Match<T>(JsonMatcher<T> matcher) where T : class
        {
            return matcher.OnBigInt(Value);
        }
    }

    public sealed record JSingle(float Value) : IJsonVal
    {
        public T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
            Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
            Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj, Func<Root, T> onRoot)
        {
            return onFloat(Value);
        }

        public T? Match<T>(JsonMatcher<T> matcher) where T : class
        {
            return matcher.OnFloat(Value);
        }
    }

    public sealed record JDouble(double Value) : IJsonVal
    {
        public T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
            Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
            Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj, Func<Root, T> onRoot)
        {
            return onDouble(Value);
        }

        public T? Match<T>(JsonMatcher<T> matcher) where T : class
        {
            return matcher.OnDouble(Value);
        }
    }

    public sealed record JBigDecimal(BigDecimal Value) : IJsonVal
    {
        public T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
            Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
            Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj, Func<Root, T> onRoot)
        {
            return onBigDecimal(Value);
        }

        public T? Match<T>(JsonMatcher<T> matcher) where T : class
        {
            return matcher.OnBigDecimal(Value);
        }
    }

    public sealed record JStr(string Value) : IJsonVal
    {
        public T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
            Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
            Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj, Func<Root, T> onRoot)
        {
            return onString(Value);
        }

        public T? Match<T>(JsonMatcher<T> matcher) where T : class
        {
            return matcher.OnString(Value);
        }
    }

    public sealed record JArr(OneArrTable Value) : IJsonVal
    {
        public T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
            Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
            Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj, Func<Root, T> onRoot)
        {
            return onArray(Value);
        }

        public T? Match<T>(JsonMatcher<T> matcher) where T : class
        {
            return matcher.OnArray(Value);
        }
    }

    public sealed record JObj(OneObjTable Value) : IJsonVal
    {
        public T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
            Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
            Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj, Func<Root, T> onRoot)
        {
            return onObj(Value);
        }

        public T? Match<T>(JsonMatcher<T> matcher) where T : class
        {
            return matcher.OnObj(Value);
        }
    }

    public sealed record JRoot(Root Value) : IJsonVal
    {
        public T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
            Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
            Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<OneArrTable, T> onArray,
            Func<OneObjTable, T> onObj, Func<Root, T> onRoot)
        {
            return onRoot(Value);
        }

        public T? Match<T>(JsonMatcher<T> matcher) where T : class
        {
            return matcher.OnRoot(Value);
        }
    }
}