#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SickSharp.Format.Tables;

namespace SickSharp
{
    public sealed partial class SickReader
    {
        internal Ref ReadObjectFieldRef(Ref reference, string field)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("ReadObjectFieldRef", reference, field))
#endif
            {
                if (reference.Kind != RefKind.Obj)
                {
                    throw new KeyNotFoundException(
                        $"Tried to find field `{field}` in entity with id `{reference}` which should be an object, but it was `{reference.Kind}`"
                    );
                }

                try
                {
                    var currentObj = _objs.Read(reference.Value);
                    var ret = ReadObjectFieldRef(currentObj, field);
#if SICK_PROFILE_READER
                    return cp.OnReturn(ret);
#else
                    return ret;
#endif
                }
                catch (Exception ex)
                {
                    throw new KeyNotFoundException($"Can not read `{field}` of `{reference}` object.", ex);
                }
            }
        }

        internal Ref ReadObjectFieldRef(OneObjTable obj, string field)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("ReadObjectFieldRef/obj", field, currentObj, clue.Value))
#endif
            {
                var lower = 0;
                var upper = obj.Count;

                // lookup index buckets only if there is more than 1 element, and object index in use 
                if (obj is { Count: > 1, UseIndex: true })
                {
                    var (_, bucket) = KHash.Bucket(field, _header.Settings.BucketSize);
                    var probablyLower = obj.BucketValue(bucket);

                    if (probablyLower == ObjIndexing.MaxIndex)
                    {
                        throw new KeyNotFoundException(
                            $"Field `{field}` not found in object `{obj}`."
                        );
                    }

                    if (probablyLower >= obj.Count)
                    {
                        throw new FormatException(
                            $"Structural failure: Field `{field}` in object `{obj}` produced bucket index `{probablyLower}` which is more than object size `{obj.Count}`."
                        );
                    }

                    lower = probablyLower;

                    // with optimized index there should be no maxIndex elements in the index, and we expect to make exactly ONE iteration
                    for (var i = bucket + 1; i < _header.Settings.BucketCount; i++)
                    {
                        var probablyUpper = obj.BucketValue(i);

                        if (probablyUpper <= obj.Count)
                        {
                            upper = probablyUpper;
                            break;
                        }

                        if (probablyUpper == ObjIndexing.MaxIndex)
                        {
                            continue;
                        }

                        if (probablyUpper > obj.Count)
                        {
                            throw new FormatException(
                                $"Field `{field}` in object `{obj}` produced bucket index `{probablyUpper}` which is more than object size `{obj.Count}`."
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
                    var refBytes = obj.ReadKeyRefSpan(i, out var key);
                    if (key != field) continue;

#if SICK_DEBUG_TRAVEL
                    TotalTravel += (i - lower);
#endif

#if SICK_PROFILE_READER
                    return cp.OnReturn(OneObjTable.ConvertRef(refBytes));
#else
                    return OneObjTable.ConvertRef(refBytes);
#endif
                }

                throw new KeyNotFoundException(
                    $"Field `{field}` not found in object `{obj}`."
                );
            }
        }

        internal SickJson ReadObjectField(Ref reference, string field)
        {
            var fieldRef = ReadObjectFieldRef(reference, field);
            return Resolve(fieldRef);
        }

        internal Ref ReadArrayElementRef(Ref reference, int index)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("ReadObjectFieldRef", reference, field))
#endif
            {
                if (reference.Kind != RefKind.Arr)
                {
                    throw new KeyNotFoundException(
                        $"Tried to find element `{index}` in entity with id `{reference}` which should be an array, but it was `{reference.Kind}`"
                    );
                }

                var currentObj = _arrs.Read(reference.Value);
                var ret = ReadArrayElementRef(currentObj, index);

#if SICK_PROFILE_READER
                return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }

        internal Ref ReadArrayElementRef(OneArrTable arr, int index)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("ReadArrayElementRef/obj", reference, iindex))
#endif
            {
                var adjustedIndex = index >= 0 ? index : arr.Count + index; // + decrements here because index is negative
                if (adjustedIndex < 0 || adjustedIndex > arr.Count - 1)
                {
                    throw new KeyNotFoundException($"Index [{index}] not found in object `{arr}`.");
                }

                var ret = arr.Read(adjustedIndex);
#if SICK_PROFILE_READER
                return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }
    }
}