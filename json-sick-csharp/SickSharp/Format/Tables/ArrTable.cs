using System;
using System.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public class ArrTable : BasicVarTable<OneArrTable>
    {
        public ArrTable(Stream stream, UInt32 offset) : base(stream, offset)
        {
        }

        protected override OneArrTable BasicRead(UInt32 absoluteStartOffset, UInt32 byteLen)
        {
            return new OneArrTable(Stream, absoluteStartOffset);
        }
    }

    public class OneArrTable : FixedTable<Ref>
    {
        public OneArrTable(Stream stream, UInt32 offset) : base(stream)
        {
            SetStart(offset);
            ReadStandardCount();
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