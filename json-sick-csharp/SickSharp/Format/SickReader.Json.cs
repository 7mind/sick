#nullable enable
using System.IO;
using Newtonsoft.Json.Linq;

namespace SickSharp
{
    public sealed partial class SickReader
    {
        public JToken ToJson(Ref reference)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("ToJson", reference))
#endif
            {
                JToken ret = reference.Kind switch
                {
                    RefKind.Nul => JValue.CreateNull(),
                    RefKind.Bit => new JValue(reference.Value == 1),
                    RefKind.SByte => new JValue((sbyte)reference.Value),
                    RefKind.Short => new JValue((short)reference.Value),
                    RefKind.Int => new JValue(Ints.Read(reference.Value)),
                    RefKind.Lng => new JValue(Longs.Read(reference.Value)),
                    RefKind.BigInt => new JValue(BigIntegers.Read(reference.Value)),
                    RefKind.Float => new JValue(Floats.Read(reference.Value)),
                    RefKind.Double => new JValue(Doubles.Read(reference.Value)),
                    RefKind.BigDec => new JValue(BigDecimals.Read(reference.Value)),
                    RefKind.String => new JValue(Strings.Read(reference.Value)),
                    RefKind.Array => ToJsonArray(reference),
                    RefKind.Object => ToJsonObject(reference),
                    RefKind.Root => ToJson(Root.Read(reference.Value).Reference),
                    _ => throw new InvalidDataException($"BUG: Unknown reference: `{reference}`")
                };

#if SICK_PROFILE_READER
                return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }

        private JToken ToJsonArray(Ref reference)
        {
            var jArray = new JArray();
            foreach (var element in Arrs.Read(reference.Value).Content())
            {
                jArray.Add(ToJson(element));
            }

            return jArray;
        }

        private JToken ToJsonObject(Ref reference)
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