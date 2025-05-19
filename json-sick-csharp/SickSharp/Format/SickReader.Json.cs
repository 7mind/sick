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
                    SickKind.Int => new JValue(Ints.Read(reference.Value)),
                    SickKind.Long => new JValue(Longs.Read(reference.Value)),
                    SickKind.BigInt => new JValue(BigIntegers.Read(reference.Value)),
                    SickKind.Float => new JValue(Floats.Read(reference.Value)),
                    SickKind.Double => new JValue(Doubles.Read(reference.Value)),
                    SickKind.BigDec => new JValue(BigDecimals.Read(reference.Value)),
                    SickKind.String => new JValue(Strings.Read(reference.Value)),
                    SickKind.Array => ToJsonArray(reference),
                    SickKind.Object => ToJsonObject(reference),
                    SickKind.Root => ToJson(Root.Read(reference.Value).Reference),
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
            foreach (var element in Arrs.Read(reference.Value).Content())
            {
                jArray.Add(ToJson(element));
            }

            return jArray;
        }

        private JToken ToJsonObject(SickRef reference)
        {
            var jObject = new JObject();
            foreach (var (key, value) in Objs.Read(reference.Value).Content())
            {
                jObject.Add(new JProperty(key, ToJson(value)));
            }

            return jObject;
        }
    }
}