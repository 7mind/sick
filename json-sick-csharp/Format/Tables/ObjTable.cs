using System.Collections.Generic;
using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    internal class ObjTable : BasicVarTable<OneObjTable>
    {
        private readonly StringTable _strings;

        public ObjTable(Stream stream, StringTable strings, int offset) : base(stream, offset)
        {
            _strings = strings;
        }

        protected override OneObjTable BasicRead(int absoluteStartOffset, int byteLen)
        {
            return new OneObjTable(Stream, _strings, absoluteStartOffset);
        }
    }

    public class OneObjTable : FixedTable<ObjEntry>
    {
        private readonly StringTable _strings;

        public OneObjTable(Stream stream, StringTable strings, int offset) : base(stream, offset)
        {
            _strings = strings;
        }

        protected override short ElementByteLength()
        {
            return sizeof(byte) + 2 * sizeof(int);
        }

        protected override ObjEntry Convert(byte[] bytes)
        {
            var keyval = bytes[..sizeof(int)].ReadInt32();
            var kind = (RefKind)bytes[sizeof(int)];

            var value = bytes[(sizeof(int) + 1)..(sizeof(int) * 2 + 1)].ReadInt32();
            return new ObjEntry(keyval, new Ref(kind, value));
        }

        public KeyValuePair<string, Ref> ReadKey(int index)
        {
            var obj = Read(index);
            return new KeyValuePair<string, Ref>(_strings.Read(obj.Key), obj.Value);
        }

        public IEnumerator<KeyValuePair<string, Ref>> GetEnumerator()
        {
            return Content().GetEnumerator();
        }
        public IEnumerable<KeyValuePair<string, Ref>> Content()
        {
            for (var i = 0; i < Count; i++) 
            {
                yield return ReadKey(i);
            };
        }
    }

    public record ObjEntry(int Key, Ref Value);
}