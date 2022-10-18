#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using SickSharp.Format.Tables;

namespace SickSharp.Encoder
{
    public struct Option<T>
    {
        public static Option<T> None => default;
        public static Option<T> Some(T value) => new Option<T>(value);

        readonly bool isSome;
        readonly T value;

        Option(T value)
        {
            this.value = value;
            isSome = this.value is { };
        }

        public bool IsSome(out T value)
        {
            value = this.value;
            return isSome;
        }
        
        public B Match<B>(Func<T, B> Some, Func<B> None) =>
            isSome
                ? Some(value)
                : None();

        public T UsafeGet()
        {
            if (isSome)
            {
                return value; 
            }

            throw new KeyNotFoundException();
        }
    }

    public static class OutExtensions
    {
        public static Option<V> TryGetValue<K, V>(this IDictionary<K, V> self, K Key)
        {
            V value;
            return self.TryGetValue(Key, out value)
                ? Option<V>.Some(value)
                : Option<V>.None;
        }
    }

    class ListComparer<T> : IEqualityComparer<List<T>>
    {
        public bool Equals(List<T> x, List<T> y)
        {
            return x.SequenceEqual(y);
        }

        public int GetHashCode(List<T> obj)
        {
            int hashcode = 0;
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
    
    public class Bijection<V>
    {
        public string Name { get; }
        private readonly Dictionary<int, V> _mapping;
        private readonly Dictionary<V, int> _reverse;
        private readonly Dictionary<int, int> _counters;
        private readonly IEqualityComparer<V>? _comparer;

        public Bijection(string name, Dictionary<int, V> mapping, Dictionary<V, int> reverse, Dictionary<int, int> counters, IEqualityComparer<V>? comparer)
        {
            Name = name;
            _mapping = mapping;
            _reverse = reverse;
            _counters = counters;
            _comparer = comparer;
        }
        
        public Option<V> Get(int idx)
        {
            return _mapping.TryGetValue(Key: idx);
        }

        public Option<int> RevGet(V value)
        {
            return _reverse.TryGetValue(Key: value);
        }

        public List<V> AsList()
        {
            if (Size() > 0)
            {
                return Enumerable.Range(0, Size()).Select(idx => _mapping[idx]).ToList();
            }

            return new List<V>();
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

        public Bijection<V> Rewrite(Func<V, V> mapping)
        {
            var rev = _comparer != null ? _reverse.ToDictionary(kv => mapping(kv.Key), kv => kv.Value, _comparer) : _reverse.ToDictionary(kv => mapping(kv.Key), kv => kv.Value);
            return new Bijection<V>(
                Name,
                _mapping.ToDictionary(kv => kv.Key, kv => mapping(kv.Value)),
                rev,
                _counters.ToDictionary(kv => kv.Key, kv => kv.Value),
                _comparer
            );
        }
        
        public int Add(V value)
        {
            if (_reverse.ContainsKey(value))
            {
                var ret = _reverse[value];
                _counters[ret] += 1;
                return ret;
            }

            var idx = _mapping.Count;
            _mapping.Add(idx, value);
            _reverse.Add(value, idx);
            _counters[idx] = 1;
            return idx;
        }
        
        public static Bijection<V> Create(string name, IEqualityComparer<V>? comparer) 
        {
            if (comparer == null)
            {
                return new Bijection<V>(name, 
                    new Dictionary<int, V>(), 
                    new Dictionary<V, int>(),
                    new Dictionary<int, int>(),
                    null
                );
            }
            else
            {
                return new Bijection<V>(name, 
                    new Dictionary<int, V>(), 
                    new Dictionary<V, int>(comparer),
                    new Dictionary<int, int>(),
                    comparer
                );

            }
        }

        public static Bijection<V> FromMonothonic(string name, List<(int Idx, V Value, int Freq)> content, IEqualityComparer<V>? comparer) 
        {

            var data = content.ToDictionary(v => v.Idx, v => v.Value);
            var revdata = comparer != null ?  content.ToDictionary(v => v.Value, v => v.Idx, comparer) : content.ToDictionary(v => v.Value, v => v.Idx);
            var freq = content.ToDictionary(v => v.Idx, v => v.Freq);

            return new Bijection<V>(name, data, revdata, freq, comparer);
        }
    }

    public interface IRemappable<V>
    {
        public V Remap(V value, Dictionary<Ref, Ref> mapping);
    }
    
    public static class BijectionExt
    {
        public static Bijection<V> Rewrite<V>(this Bijection<V> src, IRemappable<V> mapper, Dictionary<Ref, Ref> mapping) where V : class
        {
            return src.Rewrite(v => mapper.Remap(v, mapping));
        }
    }
    


}