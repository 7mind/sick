#nullable enable
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace SickSharp.Format.Tables
{
    public class StringTable : VarTable<string>
    {
        private readonly ConcurrentDictionary<int, WeakReference<string>> _cache = new();
        
        public StringTable(Stream stream, int offset) : base(stream, offset)
        {
        }

        protected override string Convert(ReadOnlySpan<byte> bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        protected override string BasicRead(int absoluteStartOffset, int byteLen)
        {
            // While it's technically possible, we don't have overlapping packing, 
            // so it's safe to use start offsets as keys
            
            // Convert(Stream.ReadSpan(absoluteStartOffset, byteLen))
            string? ret;
            
            var r = _cache.AddOrUpdate(absoluteStartOffset, (i) =>
            {
                ret = base.BasicRead(i, byteLen);
                return new WeakReference<string>(ret);
            }, (i, reference) =>
            {
                if (reference.TryGetTarget(out _))
                {
                    return reference;
                }

                ret = base.BasicRead(i, byteLen);
                return new WeakReference<string>(ret);
            });
            

            if (r.TryGetTarget(out var retw))
            {
                return retw;
            }

            // in theory, this should never happen, because in both cases `ret` will retain the strong reference
            ret = base.BasicRead(absoluteStartOffset, byteLen);
            _cache[absoluteStartOffset] = new WeakReference<string>(ret);
            
            return ret;
        }
    }
}