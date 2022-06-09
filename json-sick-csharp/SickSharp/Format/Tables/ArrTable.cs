using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public class ArrTable : BasicVarTable<OneArrTable>
    {
        public ArrTable(Stream stream, int offset) : base(stream, offset)
        {
        }

        protected override OneArrTable BasicRead(int absoluteStartOffset, int byteLen)
        {
            return new OneArrTable(Stream, absoluteStartOffset);
        }
    }

    public class OneArrTable : FixedTable<Ref>
    {
        public OneArrTable(Stream stream, int offset) : base(stream, offset)
        {
        }

        protected override short ElementByteLength()
        {
            return sizeof(byte) + sizeof(int);
        }

        protected override Ref Convert(byte[] bytes)
        {
            var kind = (RefKind)bytes[0];
            var value = bytes[1..(sizeof(int) + 1)].ReadInt32();
            return new Ref(kind, value);
        }
    }
}