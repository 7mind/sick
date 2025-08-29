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
         * <returns>Cursor to the requested root. Cursor value evaluated lazily and requires active SickReader.</returns>
         */
        public SickCursor ReadRoot(string id)
        {
            ThrowIfDisposed();

#if SICK_PROFILE_READER
            using (var cp = Profiler.OnInvoke("ReadRoot()", id))
#endif
            {
                if (!_roots.TryGetValue(id, out var reference))
                {
                    throw new KeyNotFoundException($"Root `{id}` was not found.");
                }

                var result = GetCursor(reference, SickPath.Single(id));

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
         * <param name="value">Cursor to the requested root. Cursor value evaluated lazily and requires active SickReader.</param>
         */
        public bool TryReadRoot(string id, out SickCursor value)
        {
            ThrowIfDisposed();

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

        public override SickCursor Read(params string[] path)
        {
            ThrowIfDisposed();

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

        public override SickCursor Read(ReadOnlySpan<string> path)
        {
            ThrowIfDisposed();

#if SICK_PROFILE_READER
            using (var cp = Profiler.OnInvoke("ReadSpan()", new Lazy<string>(string.Join(".", path.ToArray()))))
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

        /**
         * Resolve cursor for the specified reference.
         * <param name="reference">Reference where cursor should point.</param>
         * <param name="path">Path to reference.</param>
         */
        public SickCursor GetCursor(SickRef reference, SickPath path)
        {
            ThrowIfDisposed();

#if SICK_PROFILE_READER
            using (var trace = Profiler.OnInvoke("Resolve()", reference))
#endif
            {
                SickCursor ret = reference.Kind switch
                {
                    SickKind.Null => new SickCursor.Null(this, reference, path),
                    SickKind.Bit => new SickCursor.Bool(this, reference, path),
                    SickKind.SByte => new SickCursor.SByte(this, reference, path),
                    SickKind.Short => new SickCursor.Short(this, reference, path),
                    SickKind.Int => new SickCursor.Int(this, reference, path),
                    SickKind.Long => new SickCursor.Long(this, reference, path),
                    SickKind.BigInt => new SickCursor.BigInt(this, reference, path),
                    SickKind.Float => new SickCursor.Float(this, reference, path),
                    SickKind.Double => new SickCursor.Double(this, reference, path),
                    SickKind.BigDec => new SickCursor.BigDec(this, reference, path),
                    SickKind.String => new SickCursor.String(this, reference, path),
                    SickKind.Array => new SickCursor.Array(this, reference, path),
                    SickKind.Object => new SickCursor.Object(this, reference, path),
                    SickKind.Root => new SickCursor.Root(this, reference, path),
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