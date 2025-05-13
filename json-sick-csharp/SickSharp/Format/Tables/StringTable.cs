using System;
using System.IO;
using System.Text;

namespace SickSharp.Format.Tables
{
    public class StringTable : VarTable<string>
    {
        public StringTable(Stream stream, int offset) : base(stream, offset)
        {
        }

        protected override string Convert(ReadOnlySpan<byte> bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}