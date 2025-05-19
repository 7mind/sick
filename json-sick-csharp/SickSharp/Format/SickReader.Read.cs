#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace SickSharp
{
    public sealed partial class SickReader
    {
        /**
         * Read root value with specified identifier.
         * <param name="id">Root identifier.</param>
         */
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

        /**
         * Read root value with specified identifier.
         * <param name="id">Root identifier.</param>
         * <param name="value">Output value</param>
         */
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

        public override SickJson Read(params string[] path)
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

        public override SickJson Read(ReadOnlySpan<string> path)
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

        internal SickJson Resolve(SickRef reference)
        {
#if SICK_PROFILE_READER
            using (var trace = Profiler.OnInvoke("Resolve()", reference))
#endif
            {
                SickJson ret = reference.Kind switch
                {
                    SickKind.Null => new SickJson.Null(this, reference),
                    SickKind.Bit => new SickJson.Bool(this, reference),
                    SickKind.SByte => new SickJson.SByte(this, reference),
                    SickKind.Short => new SickJson.Short(this, reference),
                    SickKind.Int => new SickJson.Int(this, reference),
                    SickKind.Long => new SickJson.Long(this, reference),
                    SickKind.BigInt => new SickJson.BigInt(this, reference),
                    SickKind.Float => new SickJson.Float(this, reference),
                    SickKind.Double => new SickJson.Double(this, reference),
                    SickKind.BigDec => new SickJson.BigDec(this, reference),
                    SickKind.String => new SickJson.String(this, reference),
                    SickKind.Array => new SickJson.Array(this, reference),
                    SickKind.Object => new SickJson.Object(this, reference),
                    SickKind.Root => new SickJson.Root(this, reference),
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