#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SickSharp.Encoder
{
    internal readonly struct Option<T>
    {
        public static Option<T> None => default;
        public static Option<T> Some(T value) => new(value);

        private readonly bool _isSome;
        private readonly T _value;

        public Option(T value)
        {
            _value = value;
            _isSome = _value is not null;
        }

        public bool IsSome(out T value)
        {
            value = _value;
            return _isSome;
        }

        public TB Match<TB>(Func<T, TB> onSome, Func<TB> onNone)
        {
            return _isSome ? onSome(_value) : onNone();
        }

        public T UsafeGet()
        {
            return _isSome ? _value : throw new KeyNotFoundException();
        }
    }

    internal static class OutExtensions
    {
        public static Option<TV> TryGetValue<TK, TV>(this IDictionary<TK, TV> self, TK key)
        {
            return self.TryGetValue(key, out var value) ? Option<TV>.Some(value) : Option<TV>.None;
        }
    }

    internal class ListComparer<T> : IEqualityComparer<List<T>>
    {
        public bool Equals(List<T> x, List<T> y)
        {
            return x.SequenceEqual(y);
        }

        public int GetHashCode(List<T> obj)
        {
            var hashcode = 0;
            foreach (T t in obj)
            {
                if (t != null)
                {
                    hashcode ^= t.GetHashCode();
                }
            }

            return hashcode;
        }
    }

    internal class Bijection<TV>
    {
        public string Name { get; }
        private readonly Dictionary<int, TV> _mapping;
        private readonly Dictionary<TV, int> _reverse;
        private readonly Dictionary<int, int> _counters;
        private readonly IEqualityComparer<TV>? _comparer;

        public Bijection(string name, Dictionary<int, TV> mapping, Dictionary<TV, int> reverse, Dictionary<int, int> counters, IEqualityComparer<TV>? comparer)
        {
            Name = name;
            _mapping = mapping;
            _reverse = reverse;
            _counters = counters;
            _comparer = comparer;
        }

        public Option<TV> Get(int idx)
        {
            return _mapping.TryGetValue(key: idx);
        }

        public Option<int> RevGet(TV value)
        {
            return _reverse.TryGetValue(key: value);
        }

        public List<TV> AsList()
        {
            if (Size() > 0)
            {
                return Enumerable.Range(0, Size()).Select(idx => _mapping[idx]).ToList();
            }

            return new List<TV>();
        }

        public int Freq(int key)
        {
            return _counters[key];
        }

        public bool IsEmpty()
        {
            return _mapping.Count > 0;
        }

        public int Size()
        {
            return _mapping.Count;
        }

        public Bijection<TV> Rewrite(Func<TV, TV> mapping)
        {
            var rev = _comparer != null ? _reverse.ToDictionary(kv => mapping(kv.Key), kv => kv.Value, _comparer) : _reverse.ToDictionary(kv => mapping(kv.Key), kv => kv.Value);
            return new Bijection<TV>(
                Name,
                _mapping.ToDictionary(kv => kv.Key, kv => mapping(kv.Value)),
                rev,
                _counters.ToDictionary(kv => kv.Key, kv => kv.Value),
                _comparer
            );
        }

        public int Add(TV value)
        {
            if (_reverse.TryGetValue(value, out var ret))
            {
                _counters[ret] += 1;
                return ret;
            }

            var idx = _mapping.Count;
            _mapping.Add(idx, value);
            _reverse.Add(value, idx);
            _counters[idx] = 1;
            return idx;
        }

        public static Bijection<TV> Create(string name, IEqualityComparer<TV>? comparer)
        {
            if (comparer == null)
            {
                return new Bijection<TV>(name,
                    new Dictionary<int, TV>(),
                    new Dictionary<TV, int>(),
                    new Dictionary<int, int>(),
                    null
                );
            }
            else
            {
                return new Bijection<TV>(name,
                    new Dictionary<int, TV>(),
                    new Dictionary<TV, int>(comparer),
                    new Dictionary<int, int>(),
                    comparer
                );
            }
        }

        public static Bijection<TV> FromMonothonic(string name, List<(int Idx, TV Value, int Freq)> content, IEqualityComparer<TV>? comparer)
        {
            var data = content.ToDictionary(v => v.Idx, v => v.Value);
            var revdata = comparer != null ? content.ToDictionary(v => v.Value, v => v.Idx, comparer) : content.ToDictionary(v => v.Value, v => v.Idx);
            var freq = content.ToDictionary(v => v.Idx, v => v.Freq);

            return new Bijection<TV>(name, data, revdata, freq, comparer);
        }
    }

    internal interface IRemappable<TV>
    {
        public TV Remap(TV value, Dictionary<SickRef, SickRef> mapping);
    }

    internal static class BijectionExt
    {
        public static Bijection<TV> Rewrite<TV>(this Bijection<TV> src, IRemappable<TV> mapper, Dictionary<SickRef, SickRef> mapping) where TV : class
        {
            return src.Rewrite(v => mapper.Remap(v, mapping));
        }
    }
}