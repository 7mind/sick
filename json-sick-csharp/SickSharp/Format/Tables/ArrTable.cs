using System;
using System.Collections.Generic;
using SickSharp.IO;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    internal sealed class ArrTable : BasicVarTable<OneArrTable>
    {
        public ArrTable(ISickStream stream, int offset, bool loadIndexes) : base(stream, offset, loadIndexes)
        {
        }

        protected override OneArrTable BasicRead(int absoluteStartOffset, int byteLen)
        {
            return new OneArrTable(Stream, absoluteStartOffset);
        }
    }

    public sealed class OneArrTable : FixedTable<SickRef>
    {
        public OneArrTable(ISickStream stream, int offset) : base(stream)
        {
            SetStart(offset);
            ReadStandardCount();
        }

        protected override short ElementByteLength()
        {
            return sizeof(byte) + sizeof(int);
        }

        protected override SickRef Convert(ReadOnlySpan<byte> bytes)
        {
            var kind = (SickKind)bytes[0];
            var value = bytes[1..(sizeof(int) + 1)].ReadInt32BE();
            return new SickRef(kind, value);
        }

        public IEnumerator<SickRef> GetEnumerator()
        {
            return Content().GetEnumerator();
        }

        public IEnumerable<SickRef> Content()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return Read(i);
            }
        }
    }
}