#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace SickSharp
{
    public sealed partial class SickReader
    {
        public SickJson ReadRoot(string id)
        {
#if SICK_PROFILE_READER
            using (var cp = Profiler.OnInvoke("ReadRoot()", id))
#endif
            {
                if (!_roots.TryGetValue(id, out var reference))
                {
                    throw new KeyNotFoundException($"Root `{id}` was not found.");
                }

                var result = Resolve(reference);

#if SICK_PROFILE_READER
                return cp.OnReturn(result);
#else
                return result;
#endif
            }
        }

        public bool TryReadRoot(string id, out SickJson value)
        {
            try
            {
                value = ReadRoot(id);
                return true;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }

        public SickJson Read(params string[] path)
        {
#if SICK_PROFILE_READER
            using (var cp = Profiler.OnInvoke("Read()", path))
#endif
            {
                if (path.Length < 1)
                {
                    throw new ArgumentException("Can not be empty.", nameof(path));
                }

                // split path if there is only one field
                path = path.Length == 1 ? path[0].Split(".") : path;

                var root = ReadRoot(path[0]);
                if (path.Length == 1)
                {
                    return root;
                }

                var result = root.Read(new ReadOnlySpan<string>(path, 1, path.Length - 1));

#if SICK_PROFILE_READER
                return cp.OnReturn(result);
#else
                return result;
#endif
            }
        }

        public SickJson Read(ReadOnlySpan<string> path)
        {
#if SICK_PROFILE_READER
            using (var cp = Profiler.OnInvoke("ReadSpan()", path))
#endif
            {
                if (path.Length < 1)
                {
                    throw new ArgumentException("Can not be empty.", nameof(path));
                }

                var root = ReadRoot(path[0]);
                if (path.Length == 1)
                {
                    return root;
                }

                var result = root.Read(path.Slice(1, path.Length - 1));

#if SICK_PROFILE_READER
                return cp.OnReturn(result);
#else
                return result;
#endif
            }
        }

        internal SickJson Resolve(Ref reference)
        {
#if SICK_PROFILE_READER
            using (var trace = Profiler.OnInvoke("Resolve()", reference))
#endif
            {
                SickJson ret = reference.Kind switch
                {
                    RefKind.Nul => new SickJson.Null(this, reference),
                    RefKind.Bit => new SickJson.Bool(this, reference),
                    RefKind.SByte => new SickJson.SByte(this, reference),
                    RefKind.Short => new SickJson.Short(this, reference),
                    RefKind.Int => new SickJson.Int(this, reference),
                    RefKind.Lng => new SickJson.Long(this, reference),
                    RefKind.BigInt => new SickJson.BigInt(this, reference),
                    RefKind.Float => new SickJson.Float(this, reference),
                    RefKind.Double => new SickJson.Double(this, reference),
                    RefKind.BigDec => new SickJson.BigDec(this, reference),
                    RefKind.String => new SickJson.String(this, reference),
                    RefKind.Array => new SickJson.Array(this, reference),
                    RefKind.Object => new SickJson.Object(this, reference),
                    RefKind.Root => new SickJson.Root(this, reference),
                    _ => throw new InvalidDataException($"BUG: Unknown reference: `{reference}`")
                };

#if SICK_PROFILE_READER
                return trace.OnReturn(ret);
#else
                return ret;
#endif
            }
        }
    }
}