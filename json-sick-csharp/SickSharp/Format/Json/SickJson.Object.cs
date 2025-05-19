#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using SickSharp.Format.Tables;

namespace SickSharp
{
    public abstract partial class SickJson
    {
        public sealed class Object : Lazy<OneObjTable>
        {
            internal Object(SickReader reader, SickRef reference) : base(reader, SickKind.Object, reference)
            {
            }

            protected override OneObjTable Create()
            {
                return Reader.Objs.Read(Ref.Value);
            }

            public int Count => Value.Count;

            public override T Match<T>(Func<T> onNull, Func<bool, T> onBool, Func<sbyte, T> onByte, Func<short, T> onShort,
                Func<int, T> onInt, Func<long, T> onLong, Func<BigInteger, T> onBigInt, Func<float, T> onFloat,
                Func<double, T> onDouble, Func<BigDecimal, T> onBigDecimal, Func<string, T> onString, Func<Array, T> onArray,
                Func<Object, T> onObj, Func<SickSharp.SickRoot, T> onRoot)
            {
                return onObj(this);
            }

            public override T? Match<T>(SickJsonMatcher<T> matcher) where T : class
            {
                return matcher.OnObj(this);
            }

            /**
             * Enumerate all object field references.
             * Use with caution.
             */
            public IEnumerable<KeyValuePair<string, SickRef>> GetReferences()
            {
                return Value.Content();
            }

            /**
             * Enumerate all object fields.
             * Use with caution.
             */
            public IEnumerable<KeyValuePair<string, SickJson>> GetValues()
            {
                foreach (var (fieldKey, fieldRef) in Value.Content())
                {
                    yield return new KeyValuePair<string, SickJson>(fieldKey, Reader.Resolve(fieldRef));
                }
            }

            public override SickJson Query(string query)
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

            public override SickJson Read(ReadOnlySpan<string> path)
            {
#if SICK_PROFILE_READER
                using (var trace = Reader.Profiler.OnInvoke("Object.ReadSpan()", Value))
#endif
                {
                    try
                    {
                        if (path.Length == 0)
                        {
                            return this;
                        }

                        var next = ReadField(path[0]);
                        var result = path.Length == 1 ? next : next.Read(path.Slice(1, path.Length - 1));

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

            public override SickJson Read(params string[] path)
            {
#if SICK_PROFILE_READER
                using (var trace = Reader.Profiler.OnInvoke("Object.Read()", Value))
#endif
                {
                    if (path.Length == 0)
                    {
                        return this;
                    }

                    var result = path.Length == 1 ? ReadField(path[0]) : Read(new ReadOnlySpan<string>(path));

#if SICK_PROFILE_READER
                    return trace.OnReturn(result);
#else
                    return result;
#endif
                }
            }

            public override KeyValuePair<string, SickJson> ReadKey(int index)
            {
                var (key, reference) = Value.ReadKey(index);
                return new KeyValuePair<string, SickJson>(key, Reader.Resolve(reference));
            }

            public override SickJson ReadIndex(int index)
            {
                var reference = Value.ReadRef(index);
                return Reader.Resolve(reference);
            }

            public override Object AsObject()
            {
                return this;
            }

            private SickJson ReadField(string field)
            {
#if SICK_PROFILE_READER
                using (var cp = reader.Profiler.OnInvoke("Object.ReadField()", field, Value))
#endif
                {
                    var reference = ReadFieldRef(field);
                    var result = Reader.Resolve(reference);

#if SICK_PROFILE_READER
                    return cp.OnReturn(result);
#else
                    return result;
#endif
                }
            }

            private SickRef ReadFieldRef(string field)
            {
#if SICK_PROFILE_READER
                using (var cp = reader.Profiler.OnInvoke("Object.ReadFieldRef()", field, Value))
#endif
                {
                    var lower = 0;
                    var upper = Value.Count;

                    // lookup index buckets only if there is more than 1 element, and object index in use 
                    if (Value is { Count: > 1, UseIndex: true })
                    {
                        var (_, bucket) = KHash.Bucket(field, Reader.Header.Settings.BucketSize);
                        var probablyLower = Value.BucketValue(bucket);

                        if (probablyLower == ObjIndexing.MaxIndex)
                        {
                            throw new KeyNotFoundException(
                                $"Field `{field}` not found in object `{Value}`."
                            );
                        }

                        if (probablyLower >= Count)
                        {
                            throw new FormatException(
                                $"Structural failure: Field `{field}` in object `{Value}` produced bucket index `{probablyLower}` which is more than object size `{Value.Count}`."
                            );
                        }

                        lower = probablyLower;

                        // with optimized index there should be no maxIndex elements in the index, and we expect to make exactly ONE iteration
                        for (var i = bucket + 1; i < Reader.Header.Settings.BucketCount; i++)
                        {
                            var probablyUpper = Value.BucketValue(i);

                            if (probablyUpper <= Value.Count)
                            {
                                upper = probablyUpper;
                                break;
                            }

                            if (probablyUpper == ObjIndexing.MaxIndex)
                            {
                                continue;
                            }

                            if (probablyUpper > Value.Count)
                            {
                                throw new FormatException(
                                    $"Field `{field}` in object `{Value}` produced bucket index `{probablyUpper}` which is more than object size `{Value.Count}`."
                                );
                            }
                        }
                    }

#if SICK_DEBUG_TRAVEL
                    TotalLookups += 1;
#endif

                    Debug.Assert(lower <= upper);
                    for (var i = lower; i < upper; i++)
                    {
                        var refBytes = Value.ReadKeyRefSpan(i, out var key);
                        if (key != field) continue;

#if SICK_DEBUG_TRAVEL
                        TotalTravel += (i - lower);
#endif
                        var reference = OneObjTable.ConvertRef(refBytes);

#if SICK_PROFILE_READER
                        return cp.OnReturn(reference);
#else
                        return reference;
#endif
                    }

                    throw new KeyNotFoundException(
                        $"Field `{field}` not found in object `{Value}`."
                    );
                }
            }
        }
    }
}