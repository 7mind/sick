#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SickSharp
{
    public sealed partial class SickReader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SickJson Query(Ref reference, ReadOnlySpan<string> path)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("Query/span", reference, new Lazy<string[]>(path.ToArray())))
#endif
            {
                var result = QueryRef(reference, path);
                var value = Resolve(result);
                if (value == null)
                {
                    throw new KeyNotFoundException(
                        $"Failed to query `{reference}` lookup result was `{result}` but it failed to resolve. The query was `{String.Join("->", path.ToArray())}`"
                    );
                }

#if SICK_PROFILE_READER
                return cp.OnReturn(value);
#else
                return value;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryQuery(Ref reference, ReadOnlySpan<string> path, out SickJson value)
        {
            try
            {
                value = Query(reference, path);
                return true;
            }
            catch
            {
                value = null!;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SickJson Query(Ref reference, string path)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("Query", path))
#endif
            {
                var ret = Query(reference, path.Split('.'));

#if SICK_PROFILE_READER
                return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryQuery(Ref reference, string path, out SickJson value)
        {
            try
            {
                value = Query(reference, path);
                return true;
            }
            catch
            {
                value = null!;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SickJson Query(SickJson.Object obj, ReadOnlySpan<string> path)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("Query/span", jObj, new Lazy<string[]>(string.Join(path, "."))))
#endif
            {
                try
                {
                    if (path.Length == 0)
                    {
                        return obj;
                    }

                    var currentQuery = path[0];
                    var next = HandleBracketsWithoutDot(ref currentQuery, path);
                    var resolvedObj = ReadObjectFieldRef(obj.Value, currentQuery);

                    if (next.Length == 0)
                    {
                        return Resolve(resolvedObj);
                    }

                    var ret = Query(resolvedObj, next);

#if SICK_PROFILE_READER
                return cp.OnReturn(ret);
#else
                    return ret;
#endif
                }
                catch (Exception ex)
                {
                    throw new KeyNotFoundException($"Can not read `{string.Join(",", path.ToArray())}` of `{obj}` object.", ex);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryQuery(SickJson.Object obj, Span<string> path, out SickJson value)
        {
            try
            {
                value = Query(obj, path);
                return true;
            }
            catch
            {
                value = null!;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SickJson Query(SickJson.Object obj, string path)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("Query", jObj))
#endif
            {
                var ret = Query(obj, path.Split('.'));
#if SICK_PROFILE_READER
                return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryQuery(SickJson.Object obj, string path, out SickJson value)
        {
            try
            {
                value = Query(obj, path);
                return true;
            }
            catch
            {
                value = null!;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FoundRef QueryRef(Ref reference, string path)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("QueryRef", path))
#endif
            {
                var query = path.Split('.');
                var ret = new FoundRef(QueryRef(reference, query), query);

#if SICK_PROFILE_READER
                return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryQueryRef(Ref reference, string path, out FoundRef value)
        {
            try
            {
                value = QueryRef(reference, path);
                return true;
            }
            catch
            {
                value = null!;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Ref QueryRef(Ref reference, ReadOnlySpan<string> path)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("QueryRef/span", reference, new Lazy<string[]>(path.ToArray())))
#endif
            {
                if (path.Length == 0)
                {
                    return reference;
                }

                var currentQuery = path[0];
                var next = HandleBracketsWithoutDot(ref currentQuery, path);

                if (currentQuery.StartsWith("[") && currentQuery.EndsWith("]"))
                {
                    var strIndex = currentQuery.Substring(1, currentQuery.Length - 2);
                    var index = int.Parse(strIndex);

                    var resolvedArr = ReadArrayElementRef(reference, index);
                    return QueryRef(resolvedArr, next);
                }

                var resolvedObj = ReadObjectFieldRef(reference, currentQuery);

                if (next.Length == 0)
                {
                    return resolvedObj;
                }

                return QueryRef(resolvedObj, next);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryQueryRef(Ref reference, ReadOnlySpan<string> path, out Ref value)
        {
            try
            {
                value = QueryRef(reference, path);
                return true;
            }
            catch
            {
                value = null!;
                return false;
            }
        }
    }
}