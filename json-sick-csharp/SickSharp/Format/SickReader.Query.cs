#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace SickSharp
{
    public sealed partial class SickReader
    {
        public override SickCursor Query(string query)
        {
            ThrowIfDisposed();

#if SICK_PROFILE_READER
            using (var cp = Profiler.OnInvoke("Query()", query))
#endif
            {
                if (string.IsNullOrEmpty(query))
                {
                    throw new ArgumentException("Can not be empty.", nameof(query));
                }

                var path = ParseQuery(query);
                var result = Read(path);

#if SICK_PROFILE_READER
                return cp.OnReturn(result);
#else
                return result;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<string> ParseQuery(string query)
        {
            return new ReadOnlySpan<string>(query.Split(QuerySeparators, StringSplitOptions.RemoveEmptyEntries));
        }

        private static readonly char[] QuerySeparators = { '.', '[', ']' };
    }
}