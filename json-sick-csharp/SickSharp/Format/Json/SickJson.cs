#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using SickSharp.Format.Tables;

namespace SickSharp
{
    public abstract partial class SickJson
    {
        protected readonly SickReader Reader;
        public readonly Ref Ref;
        public RefKind Kind => Ref.Kind;

        private SickJson(SickReader reader, RefKind expectedKind, Ref reference)
        {
            Debug.Assert(expectedKind == reference.Kind);
            Reader = reader;
            Ref = reference;
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

        public virtual SickJson Query(string query)
        {
            throw new KeyNotFoundException($"Can not query `{query}` from <{GetType().Name}>.");
        }

        public bool TryQuery(string query, out SickJson value)
        {
            try
            {
                value = Query(query);
                return false;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }

        public virtual SickJson Read(ReadOnlySpan<string> path)
        {
            throw new KeyNotFoundException($"Can not read field `{string.Join(".", path.ToArray())}` from <{GetType().Name}>.");
        }

        public bool TryRead(out SickJson value, ReadOnlySpan<string> path)
        {
            try
            {
                value = Read(path);
                return true;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }

        public virtual Ref ReadRef(ReadOnlySpan<string> path)
        {
            throw new KeyNotFoundException($"Can not read field `{string.Join(".", path.ToArray())}` from <{GetType().Name}>.");
        }

        public bool TryReadRef(out Ref value, ReadOnlySpan<string> path)
        {
            try
            {
                value = ReadRef(path);
                return true;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }

        public virtual SickJson Read(params string[] path)
        {
            throw new KeyNotFoundException($"Can not read field `{string.Join(".", path)}` from <{GetType().Name}>.");
        }

        public bool TryRead(out SickJson value, params string[] path)
        {
            try
            {
                value = Read(path);
                return true;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }

        public virtual Ref ReadRef(params string[] path)
        {
            throw new KeyNotFoundException($"Can not read field `{string.Join(".", path)}` from <{GetType().Name}>.");
        }

        public bool TryReadRef(out Ref value, params string[] path)
        {
            try
            {
                value = ReadRef(path);
                return true;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }

        public virtual KeyValuePair<string, SickJson> ReadKey(int index)
        {
            throw new KeyNotFoundException($"Can not read indexed key field [{index}] from <{GetType().Name}>.");
        }

        public bool TryReadKey(out KeyValuePair<string, SickJson> value, int index)
        {
            try
            {
                value = ReadKey(index);
                return true;
            }
            catch (Exception)
            {
                value = default;
                return false;
            }
        }

        public virtual SickJson ReadIndex(int index)
        {
            throw new KeyNotFoundException($"Can not read indexed field [{index}] from <{GetType().Name}>.");
        }

        public bool TryReadIndex(out SickJson value, int index)
        {
            try
            {
                value = ReadIndex(index);
                return true;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }

        public virtual bool AsBool()
        {
            throw new ArgumentException($"Can not get <bool> from <{GetType().Name}>.");
        }

        public virtual sbyte AsSByte()
        {
            throw new ArgumentException($"Can not get <sbyte> from <{GetType().Name}>.");
        }

        public virtual byte AsByte()
        {
            throw new ArgumentException($"Can not get <sbyte> from <{GetType().Name}>.");
        }

        public virtual short AsShort()
        {
            throw new ArgumentException($"Can not get <short> from <{GetType().Name}>.");
        }

        public virtual int AsInt()
        {
            throw new ArgumentException($"Can not get <int> from <{GetType().Name}>.");
        }

        public virtual long AsLong()
        {
            throw new ArgumentException($"Can not get <long> from <{GetType().Name}>.");
        }

        public virtual double AsDouble()
        {
            throw new ArgumentException($"Can not get <double> from <{GetType().Name}>.");
        }

        public virtual string AsString()
        {
            throw new ArgumentException($"Can not get <string> from <{GetType().Name}>.");
        }

        public virtual Object AsObject()
        {
            throw new ArgumentException($"Can not get <SickJson.Object> from <{GetType().Name}>.");
        }

        public virtual Array AsArray()
        {
            throw new ArgumentException($"Can not get <SickJson.Array> from <{GetType().Name}>.");
        }

        public T As<T>() where T : SickJson
        {
            if (this is T t) return t;
            throw new ArgumentException($"Can not convert to <{typeof(T).Name}> from <{GetType().Name}>.");
        }

        /**
         * Simple lazy implementation.
         * Mostly copy-paste from the System.Lazy.
         */
        public abstract class Lazy<T> : SickJson
        {
            private volatile bool _created;
            private T? _value;
            public T Value => _created ? _value! : CreateValue();

            protected Lazy(SickReader reader, RefKind expectedKind, Ref reference) : base(reader, expectedKind, reference)
            {
            }

            protected abstract T Create();

            private T CreateValue()
            {
                lock (this)
                {
                    if (!_created)
                    {
                        _created = true;
                        _value = Create();
                    }

                    return _value!;
                }
            }
        }
    }
}