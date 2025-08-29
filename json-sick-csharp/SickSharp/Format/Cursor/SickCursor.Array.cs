#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using SickSharp.Format.Tables;

namespace SickSharp
{
    public abstract partial class SickCursor
    {
        public sealed class Array : LazyCursor<OneArrTable>
        {
            internal Array(SickReader reader, SickRef reference, SickPath path) : base(reader, SickKind.Array, reference, path)
            {
            }

            protected override OneArrTable Create()
            {
                return Reader.Arrs.Read(Ref.Value);
            }

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickRoot, T> onRoot)
            {
                return onArray(this);
            }

            public override T? Match<T>(SickCursorMatcher<T> matcher) where T : class
            {
                return matcher.OnArray(this);
            }

            public int Count => Value.Count;

            public override SickCursor Query(string query)
            {
#if SICK_PROFILE_READER
                using (var trace = Reader.Profiler.OnInvoke("Object.Query()", Value))
#endif
                {
                    if (string.IsNullOrEmpty(query))
                    {
                        throw new ArgumentException("Can not be empty.", nameof(query));
                    }

                    var path = SickReader.ParseQuery(query);
                    var result = Read(path);

#if SICK_PROFILE_READER
                    return trace.OnReturn(result);
#else
                    return result;
#endif
                }
            }

            public override SickCursor Read(ReadOnlySpan<string> path)
            {
#if SICK_PROFILE_READER
                using (var trace = Reader.Profiler.OnInvoke("Array.ReadSpan()", Value))
#endif
                {
                    try
                    {
                        if (path.Length == 0)
                        {
                            return this;
                        }


                        var element = ReadIndexString(path[0]);
                        var result = path.Length == 1 ? element : element.Read(path[1..]);

#if SICK_PROFILE_READER
                        return trace.OnReturn(result);
#else
                        return result;
#endif
                    }
                    catch (Exception ex)
                    {
                        throw new KeyNotFoundException($"Can not read `{string.Join(",", path.ToArray())}` of `{Value}` object.", ex);
                    }
                }
            }

            /**
             * Enumerate all array element references.
             * Use with caution.
             * <returns>Enumerable elements references, for latter usage with SickReader.GetCursor(reference).</returns>
             */
            public IEnumerable<SickRef> GetReferences()
            {
                return Value.Content();
            }

            /**
             * Enumerate all array elements.
             * Use with caution.
             * <returns>Enumerable elements cursors. Cursor value evaluated lazily and requires active SickReader.</returns>
             */
            public IEnumerable<SickCursor> GetValues()
            {
                return Value.Content().Select((fieldRef, idx) =>
                {
                    return Reader.GetCursor(fieldRef, Path.Append(idx));
                });
            }

            public override SickCursor Read(params string[] path)
            {
#if SICK_PROFILE_READER
                using (var trace = Reader.Profiler.OnInvoke("Array.Read()", Value))
#endif
                {
                    if (path.Length == 0)
                    {
                        return this;
                    }

                    var result = path.Length == 1 ? ReadIndexString(path[0]) : Read(new ReadOnlySpan<string>(path));

#if SICK_PROFILE_READER
                    return trace.OnReturn(result);
#else
                    return result;
#endif
                }
            }


            public override SickCursor ReadIndex(int index)
            {
#if SICK_PROFILE_READER
                using (var trace = Reader.Profiler.OnInvoke("SickArray.ReadIndex()", Value, index))
#endif
                {
                    var adjustedIndex = index >= 0 ? index : Value.Count + index; // + decrements here because index is negative
                    if (adjustedIndex < 0 || adjustedIndex > Value.Count - 1)
                    {
                        throw new KeyNotFoundException($"Index [{index}] not found in object `{Value}`.");
                    }

                    var reference = Value.Read(adjustedIndex);
                    var result = Reader.GetCursor(reference, Path.Append(adjustedIndex));
#if SICK_PROFILE_READER
                    return trace.OnReturn(result);
#else
                    return result;
#endif
                }
            }

            private SickCursor ReadIndexString(string stringIndex)
            {
                var index = ParseBracketIndex(stringIndex);
                if (!index.HasValue)
                {
                    throw new KeyNotFoundException($"Can not read field `{stringIndex}` from <SickCursor.Array>.");
                }

                return ReadIndex(index.Value);
            }

            public override Array AsArray()
            {
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int? ParseBracketIndex(string index)
            {
                if (string.IsNullOrEmpty(index)) return null;
                var start = index[0] == '[' ? 1 : 0;
                var end = index[^1] == ']' ? index.Length - 1 : index.Length;
                if (int.TryParse(index.AsSpan(start, end), out var res))
                {
                    return res;
                }

                return null;
            }
        }
    }
}