#nullable enable
using System;
using System.Diagnostics;
using System.Numerics;
using SickSharp.Format.Tables;

namespace SickSharp
{
    public abstract partial class SickCursor : SickObjectReader
    {
        protected readonly SickReader Reader;
        public readonly SickRef Ref;
        public readonly SickPath Path;

        private SickCursor(SickReader reader, SickKind expectedKind, SickRef reference, SickPath path)
        {
            Debug.Assert(expectedKind == reference.Kind);
            Reader = reader;
            Ref = reference;
            Path = path;
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
            Func<SickSharp.SickRoot, T> onRoot
        );

        public abstract T? Match<T>(SickCursorMatcher<T> matcher) where T : class;

        public virtual bool IsNull => false;

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

        public virtual float AsFloat()
        {
            throw new ArgumentException($"Can not get <float> from <{GetType().Name}>.");
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
            throw new ArgumentException($"Can not get <SickCursor.Object> from <{GetType().Name}>.");
        }

        public virtual Array AsArray()
        {
            throw new ArgumentException($"Can not get <SickCursor.Array> from <{GetType().Name}>.");
        }

        /**
         * Cast JSON to specified type.
         */
        public T As<T>() where T : SickCursor
        {
            if (this is T t) return t;
            throw new ArgumentException($"Can not convert to <{typeof(T).Name}> from <{GetType().Name}>.");
        }

        /**
         * Try to cast JSON to specified type.
         */
        public bool TryAs<T>(out T value) where T : SickCursor
        {
            value = (this as T)!;
            return value != null!;
        }

        /**
         * Simple lazy implementation.
         * Mostly copy-paste from the System.Lazy.
         */
        public abstract class LazyCursor<T> : SickCursor
        {
            private volatile bool _created;
            private T? _value;

            /**
             * Value of the current cursor.
             * Value is lazily read from cursor's SickReader instance.
             */
            public T Value => _created ? _value! : CreateValue();

            protected LazyCursor(SickReader reader, SickKind expectedKind, SickRef reference, SickPath path) : base(reader, expectedKind, reference, path)
            {
            }

            protected abstract T Create();

            private T CreateValue()
            {
                lock (this)
                {
                    if (!_created)
                    {
                        _value = Create();
                        _created = true;
                    }

                    return _value!;
                }
            }
        }
    }
}