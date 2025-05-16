#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SickSharp.Format.Tables;

namespace SickSharp.Format
{
    public sealed partial class SickReader
    {
        public IJsonVal Resolve(Ref reference)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("Resolve", reference))
#endif
            {
                IJsonVal ret = reference.Kind switch
                {
                    RefKind.Nul => new JNull(),
                    RefKind.Bit => new JBool(reference.Value == 1),
                    RefKind.SByte => new JSByte((sbyte)reference.Value),
                    RefKind.Short => new JShort((short)reference.Value),
                    RefKind.Int => new JInt(Ints.Read(reference.Value)),
                    RefKind.Lng => new JLong(Longs.Read(reference.Value)),
                    RefKind.BigInt => new JBigInt(BigInts.Read(reference.Value)),
                    RefKind.Flt => new JSingle(Floats.Read(reference.Value)),
                    RefKind.Dbl => new JDouble(Doubles.Read(reference.Value)),
                    RefKind.BigDec => new JBigDecimal(BigDecimals.Read(reference.Value)),
                    RefKind.Str => new JStr(Strings.Read(reference.Value)),
                    RefKind.Arr => new JArr(Arrs.Read(reference.Value)),
                    RefKind.Obj => new JObj(Objs.Read(reference.Value)),
                    RefKind.Root => new JRoot(Roots.Read(reference.Value)),
                    _ => throw new InvalidDataException($"BUG: Unknown reference: `{reference}`")
                };

#if SICK_PROFILE_READER
                return cp.OnReturn(ret);
#else
                return ret;
#endif
            }
        }

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
                    RefKind.BigInt => new JValue(BigInts.Read(reference.Value)),
                    RefKind.Flt => new JValue(Floats.Read(reference.Value)),
                    RefKind.Dbl => new JValue(Doubles.Read(reference.Value)),
                    RefKind.BigDec => new JValue(BigDecimals.Read(reference.Value)),
                    RefKind.Str => new JValue(Strings.Read(reference.Value)),
                    RefKind.Arr => ToJsonArray(reference),
                    RefKind.Obj => ToJsonObject(reference),
                    RefKind.Root => ToJson(Roots.Read(reference.Value).Reference),
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