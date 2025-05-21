#nullable enable
using System.IO;
using Newtonsoft.Json.Linq;

namespace SickSharp
{
    public sealed partial class SickReader
    {
        /**
         * Convert opened SICK file to JSON.
         */
        public JToken ToJson(SickRef reference)
        {
            ThrowIfDisposed();

#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("ToJson", reference))
#endif
            {
                JToken ret = reference.Kind switch
                {
                    SickKind.Null => JValue.CreateNull(),
                    SickKind.Bit => new JValue(reference.Value == 1),
                    SickKind.SByte => new JValue((sbyte)reference.Value),
                    SickKind.Short => new JValue((short)reference.Value),
                    SickKind.Int => new JValue(_ints.Read(reference.Value)),
                    SickKind.Long => new JValue(_longs.Read(reference.Value)),
                    SickKind.BigInt => new JValue(_bigIntegers.Read(reference.Value)),
                    SickKind.Float => new JValue(_floats.Read(reference.Value)),
                    SickKind.Double => new JValue(_doubles.Read(reference.Value)),
                    SickKind.BigDec => new JValue(_bigDecimals.Read(reference.Value)),
                    SickKind.String => new JValue(_strings.Read(reference.Value)),
                    SickKind.Array => ToJsonArray(reference),
                    SickKind.Object => ToJsonObject(reference),
                    SickKind.Root => ToJson(_root.Read(reference.Value).Reference),
                    _ => throw new InvalidDataException($"BUG: Unknown reference: `{reference}`")
                };

#if SICK_PROFILE_READER
                return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }

        private JToken ToJsonArray(SickRef reference)
        {
            var jArray = new JArray();
            foreach (var element in _arrs.Read(reference.Value).Content())
            {
                jArray.Add(ToJson(element));
            }

            return jArray;
        }

        private JToken ToJsonObject(SickRef reference)
        {
            var jObject = new JObject();
            foreach (var (key, value) in _objs.Read(reference.Value).Content())
            {
                jObject.Add(new JProperty(key, ToJson(value)));
            }

            return jObject;
        }
    }
}