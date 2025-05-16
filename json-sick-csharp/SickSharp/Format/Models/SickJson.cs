#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SickSharp.Format.Tables;

namespace SickSharp
{
    public abstract class SickJson
    {
        private SickJson()
        {
        }

        public abstract T Match<T>(
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
            Func<Array, T> onArray,
            Func<Object, T> onObj,
            Func<SickSharp.Root, T> onRoot
        );

        public abstract T? Match<T>(SickJsonMatcher<T> matcher) where T : class;

        public sealed class Null : SickJson
        {
            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.Root, T> onRoot)
            {
                return onNull();
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnNull();
            }
        }

        public sealed class Bool : SickJson
        {
            public readonly bool Value;

            public Bool(bool value)
            {
                Value = value;
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
        }

        public sealed class SByte : SickJson
        {
            public readonly sbyte Value;

            public SByte(sbyte value)
            {
                Value = value;
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.Root, T> onRoot)
            {
                return onByte(Value);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnByte(Value);
            }
        }

        public sealed class Short : SickJson
        {
            public readonly short Value;

            public Short(short value)
            {
                Value = value;
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.Root, T> onRoot)
            {
                return onShort(Value);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnShort(Value);
            }
        }

        public sealed class Int : SickJson
        {
            public readonly int Value;

            public Int(int value)
            {
                Value = value;
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
        }

        public sealed class Long : SickJson
        {
            public readonly long Value;

            public Long(long value)
            {
                Value = value;
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.Root, T> onRoot)
            {
                return onLong(Value);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnLong(Value);
            }
        }

        public sealed class BigInt : SickJson
        {
            public readonly BigInteger Value;

            public BigInt(BigInteger value)
            {
                Value = value;
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.Root, T> onRoot)
            {
                return onBigInt(Value);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnBigInt(Value);
            }
        }

        public sealed class Single : SickJson
        {
            public readonly float Value;

            public Single(float value)
            {
                Value = value;
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
        }

        public sealed class Double : SickJson
        {
            public readonly double Value;

            public Double(double value)
            {
                Value = value;
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.Root, T> onRoot)
            {
                return onDouble(Value);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnDouble(Value);
            }
        }

        public sealed class BigDec : SickJson
        {
            public readonly BigDecimal Value;

            public BigDec(BigDecimal value)
            {
                Value = value;
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.Root, T> onRoot)
            {
                return onBigDecimal(Value);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnBigDecimal(Value);
            }
        }

        public sealed class String : SickJson
        {
            public readonly string Value;

            public String(string value)
            {
                Value = value;
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.Root, T> onRoot)
            {
                return onString(Value);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnString(Value);
            }
        }

        public sealed class Array : SickJson
        {
            private readonly SickReader _reader;
            internal readonly OneArrTable Value;

            internal Array(SickReader reader, OneArrTable value)
            {
                _reader = reader;
                Value = value;
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.Root, T> onRoot)
            {
                return onArray(this);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnArray(this);
            }

            public int Count => Value.Count;

            public IEnumerable<Ref> Content()
            {
                return Value.Content();
            }

            public IReadOnlyList<SickJson> ReadAll()
            {
                return Value.Content().Select(elemRef => _reader.Resolve(elemRef)).ToList();
            }

            public IReadOnlyList<T> ReadAll<T>() where T : SickJson
            {
                return Value.Content().Select(elemRef => (T)_reader.Resolve(elemRef)).ToList();
            }

            public SickJson Read(int index)
            {
                var elemRef = _reader.ReadArrayElementRef(Value, index);
                return _reader.Resolve(elemRef);
            }


            public T Read<T>(int index) where T : SickJson
            {
                var elemRef = _reader.ReadArrayElementRef(Value, index);
                return (T)_reader.Resolve(elemRef);
            }

            public Ref ReadRef(int index)
            {
                return _reader.ReadArrayElementRef(Value, index);
            }

            public bool TryRead(int index, out SickJson value)
            {
                try
                {
                    var elemRef = _reader.ReadArrayElementRef(Value, index);
                    value = _reader.Resolve(elemRef);
                    return true;
                }
                catch (Exception)
                {
                    value = null!;
                    return false;
                }
            }

            public bool TryRead<T>(int index, out SickJson value) where T : SickJson
            {
                try
                {
                    var elemRef = _reader.ReadArrayElementRef(Value, index);
                    value = (_reader.Resolve(elemRef) as T)!;
                    return value != null!;
                }
                catch (Exception)
                {
                    value = null!;
                    return false;
                }
            }

            public bool TryReadRef(int index, out Ref value)
            {
                try
                {
                    value = _reader.ReadArrayElementRef(Value, index);
                    return true;
                }
                catch (Exception)
                {
                    value = null!;
                    return false;
                }
            }
        }

        public sealed class Object : SickJson
        {
            private readonly SickReader _reader;
            internal readonly OneObjTable Value;

            internal Object(SickReader reader, OneObjTable value)
            {
                _reader = reader;
                Value = value;
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.Root, T> onRoot)
            {
                return onObj(this);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnObj(this);
            }

            public int Count => Value.Count;

            public IEnumerable<KeyValuePair<string, Ref>> Content()
            {
                return Value.Content();
            }

            public IReadOnlyDictionary<string, SickJson> ReadAll()
            {
                var dictionary = new Dictionary<string, SickJson>();
                foreach (var (fieldKey, fieldRef) in Value.Content())
                {
                    dictionary[fieldKey] = _reader.Resolve(fieldRef);
                }

                return dictionary;
            }

            public T Read<T>(string field) where T : SickJson
            {
                return (T)Read(field);
            }

            public SickJson Read(string field)
            {
                var fieldRef = _reader.ReadObjectFieldRef(Value, field);
                return _reader.Resolve(fieldRef);
            }

            public KeyValuePair<string, SickJson> Read(int index)
            {
                var (key, reference) = Value.ReadKey(index);
                return new KeyValuePair<string, SickJson>(key, _reader.Resolve(reference));
            }

            public KeyValuePair<string, T> Read<T>(int index) where T : SickJson
            {
                var (k, v) = Read(index);
                return new KeyValuePair<string, T>(k, (T)v);
            }

            public Ref ReadRef(string field)
            {
                return _reader.ReadObjectFieldRef(Value, field);
            }

            public bool TryRead(string field, out SickJson value)
            {
                try
                {
                    var fieldRef = _reader.ReadObjectFieldRef(Value, field);
                    value = _reader.Resolve(fieldRef);
                    return true;
                }
                catch (Exception)
                {
                    value = null!;
                    return false;
                }
            }

            public bool TryRead<T>(string field, out T value) where T : SickJson
            {
                try
                {
                    var fieldRef = _reader.ReadObjectFieldRef(Value, field);
                    value = (_reader.Resolve(fieldRef) as T)!;
                    return value != null!;
                }
                catch (Exception)
                {
                    value = null!;
                    return false;
                }
            }

            public bool TryReadRef(string field, out Ref value)
            {
                try
                {
                    value = _reader.ReadObjectFieldRef(Value, field);
                    return true;
                }
                catch (Exception)
                {
                    value = null!;
                    return false;
                }
            }
        }

        public sealed class Root : SickJson
        {
            public readonly SickSharp.Root Value;

            public Root(SickSharp.Root value)
            {
                Value = value;
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