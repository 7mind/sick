#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace SickSharp
{
    public sealed partial class SickReader
    {
        public SickJson Query(string query)
        {
#if SICK_PROFILE_READER
            using (var cp = Profiler.OnInvoke("Query()", path))
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

        public bool TryQuery(string query, out SickJson value)
        {
            try
            {
                value = Query(query);
                return true;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<string> ParseQuery(string query)
        {
            return new ReadOnlySpan<string>(query.Split(QuerySeparators, StringSplitOptions.RemoveEmptyEntries));
        }

        private static readonly char[] QuerySeparators = { '.', '[', ']' };
    }
}