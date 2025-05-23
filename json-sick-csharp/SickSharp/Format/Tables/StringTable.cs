#nullable enable
using System;
using System.Collections.Concurrent;
using System.Text;
using SickSharp.IO;

#if SICK_CACHE_STRINGS
using SickSharp.Primitives;
#endif

namespace SickSharp.Format.Tables
{
    public class StringTable : VarTable<string>
    {
#if SICK_CACHE_STRINGS
        private readonly ConcurrentDictionary<int, string> _cache = new();
#endif

        public StringTable(ISickStream stream, int offset, bool loadIndexes) : base(stream, offset, loadIndexes)
        {
        }

        protected override string Convert(ReadOnlySpan<byte> bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

#if SICK_CACHE_STRINGS
        protected override string BasicRead(int absoluteStartOffset, int byteLen)
        {
            // While it's technically possible, we don't have overlapping packing, 
            // so it's safe to use start offsets as keys
            
            // Convert(Stream.ReadSpan(absoluteStartOffset, byteLen))
            return _cache.GetOrAdd(absoluteStartOffset, (i) => Convert(Stream.ReadSpan(i, byteLen)));
        }
#endif
    }
}