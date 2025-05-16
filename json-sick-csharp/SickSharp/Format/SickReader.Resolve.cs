#nullable enable
using System.IO;
using Newtonsoft.Json.Linq;

namespace SickSharp
{
    public sealed partial class SickReader
    {
        public SickJson Resolve(Ref reference)
        {
#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("Resolve", reference))
#endif
            {
                SickJson ret = reference.Kind switch
                {
                    RefKind.Nul => new SickJson.Null(),
                    RefKind.Bit => new SickJson.Bool(reference.Value == 1),
                    RefKind.SByte => new SickJson.SByte((sbyte)reference.Value),
                    RefKind.Short => new SickJson.Short((short)reference.Value),
                    RefKind.Int => new SickJson.Int(_ints.Read(reference.Value)),
                    RefKind.Lng => new SickJson.Long(_longs.Read(reference.Value)),
                    RefKind.BigInt => new SickJson.BigInt(_bigInts.Read(reference.Value)),
                    RefKind.Flt => new SickJson.Single(_floats.Read(reference.Value)),
                    RefKind.Dbl => new SickJson.Double(_doubles.Read(reference.Value)),
                    RefKind.BigDec => new SickJson.BigDec(_bigDecimals.Read(reference.Value)),
                    RefKind.Str => new SickJson.String(_strings.Read(reference.Value)),
                    RefKind.Arr => new SickJson.Array(this, _arrs.Read(reference.Value)),
                    RefKind.Obj => new SickJson.Object(this, _objs.Read(reference.Value)),
                    RefKind.Root => new SickJson.Root(_root.Read(reference.Value)),
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
                    RefKind.Int => new JValue(_ints.Read(reference.Value)),
                    RefKind.Lng => new JValue(_longs.Read(reference.Value)),
                    RefKind.BigInt => new JValue(_bigInts.Read(reference.Value)),
                    RefKind.Flt => new JValue(_floats.Read(reference.Value)),
                    RefKind.Dbl => new JValue(_doubles.Read(reference.Value)),
                    RefKind.BigDec => new JValue(_bigDecimals.Read(reference.Value)),
                    RefKind.Str => new JValue(_strings.Read(reference.Value)),
                    RefKind.Arr => ToJsonArray(reference),
                    RefKind.Obj => ToJsonObject(reference),
                    RefKind.Root => ToJson(_root.Read(reference.Value).Reference),
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
            foreach (var element in _arrs.Read(reference.Value).Content())
            {
                jArray.Add(ToJson(element));
            }

            return jArray;
        }

        private JToken ToJsonObject(Ref reference)
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