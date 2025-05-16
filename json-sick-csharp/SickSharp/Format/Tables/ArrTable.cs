using System;
using System.Collections.Generic;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    internal sealed class ArrTable : BasicVarTable<OneArrTable>
    {
        public ArrTable(SpanStream stream, int offset, bool loadIndexes) : base(stream, offset, loadIndexes)
        {
        }

        protected override OneArrTable BasicRead(int absoluteStartOffset, int byteLen)
        {
            return new OneArrTable(Stream, absoluteStartOffset);
        }
    }

    internal sealed class OneArrTable : FixedTable<Ref>
    {
        public OneArrTable(SpanStream stream, int offset) : base(stream)
        {
            SetStart(offset);
            ReadStandardCount();
        }

        protected override short ElementByteLength()
        {
            return sizeof(byte) + sizeof(int);
        }

        protected override Ref Convert(ReadOnlySpan<byte> bytes)
        {
            var kind = (RefKind)bytes[0];
            var value = bytes[1..(sizeof(int) + 1)].ReadInt32BE();
            return new Ref(kind, value);
        }

        public IEnumerator<Ref> GetEnumerator()
        {
            return Content().GetEnumerator();
        }

        public IEnumerable<Ref> Content()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return Read(i);
            }
        }
    }
}