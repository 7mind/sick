#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using SickSharp.Format.Tables;

namespace SickSharp.Encoder
{
    public class Bijection<V>
    {
        public string Name { get; }
        private readonly Dictionary<int, V> _mapping;
        private readonly Dictionary<V, int> _reverse;
        private readonly Dictionary<int, int> _counters;

        public Bijection(string name, Dictionary<int, V> mapping, Dictionary<V, int> reverse, Dictionary<int, int> counters)
        {
            Name = name;
            _mapping = mapping;
            _reverse = reverse;
            _counters = counters;
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
            return Enumerable.Range(0, Size() - 1).Map(idx => _mapping[idx]).ToList();
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
            return new Bijection<V>(
                Name,
                _mapping.ToDictionary(kv => kv.Key, kv => mapping(kv.Value)),
                _reverse.ToDictionary(kv => mapping(kv.Key), kv => kv.Value),
                _counters.ToDictionary(kv => kv.Key, kv => kv.Value)
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
        
        
        public static Bijection<V> Create(string name) 
        {
            return new Bijection<V>(name, 
                new Dictionary<int, V>(), 
                new Dictionary<V, int>(),
                new Dictionary<int, int>()
            );
        }

        public static Bijection<V> FromMonothonic(string name, List<(int Idx, V Value, int Freq)> content) 
        {

            var data = content.ToDictionary(v => v.Idx, v => v.Value);
            var revdata = content.ToDictionary(v => v.Value, v => v.Idx);
            var freq = content.ToDictionary(v => v.Idx, v => v.Freq);

            return new Bijection<V>(name, data, revdata, freq);
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