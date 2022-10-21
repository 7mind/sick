#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using SickSharp.Primitives;

namespace SickSharp.Format.Tables
{
    public class ObjTable : BasicVarTable<OneObjTable>
    {
        private readonly StringTable _strings;
        private readonly ObjIndexing _settings;

        public ObjTable(Stream stream, StringTable strings, UInt32 offset, ObjIndexing settings) : base(stream, offset)
        {
            _strings = strings;
            _settings = settings;
        }

        protected override OneObjTable BasicRead(UInt32 absoluteStartOffset, UInt32 byteLen)
        {
            return new OneObjTable(Stream, _strings, absoluteStartOffset, _settings);
        }
    }

    public class KHash
    {
        public static Int64 Compute(string s)
        {
            Int32 a = 0x6BADBEEF;
            foreach (var b in Encoding.UTF8.GetBytes(s))
            {
                a ^= a << 13;
                a += (a ^ b) << 8;
            }

            return (long)(a) & 0xffffffffL;
        }
    }

    public class ObjIndexing
    {
        public const ushort NoIndex = 65535;
        public const ushort MaxIndex = NoIndex - 1;
        //public const ushort BucketCount = 16;
        public const long Range = (long)UInt32.MaxValue + 1;
        public readonly ushort BucketCount;
        public readonly long BucketSize;

        public ObjIndexing(ushort bucketCount)
        {
            BucketCount = bucketCount;
            BucketSize = Range / BucketCount;
            Debug.Assert(Range == 4294967296);
            Debug.Assert(BucketCount > 1);
            Debug.Assert(Range % BucketCount == 0);
        }

        public const ushort IndexMemberSize = sizeof(ushort); 
        
        
    }
    
    public class OneObjTable : FixedTable<ObjEntry>
    {
        private readonly StringTable _strings;
        


        public readonly ushort[]? Index;
        public readonly ushort[]? NextIndex;
        //public readonly Dictionary<UInt32, ushort> NextIndex;
        public bool UseIndex { get; }
        
        public OneObjTable(Stream stream, StringTable strings, UInt32 offset, ObjIndexing settings) : base(stream, offset)
        {


            _strings = strings;

            var indexHeader = ReadBytes(offset, ObjIndexing.IndexMemberSize).ReadUInt16();

            if (indexHeader == ObjIndexing.NoIndex)
            {
                Offset = offset + ObjIndexing.IndexMemberSize;
                UseIndex = false;
            } 
            else
            {
                var indexSize = settings.BucketCount * ObjIndexing.IndexMemberSize;
                Offset = (uint)(offset + indexSize) ;
                UseIndex = true;
                Index = new ushort[settings.BucketCount];
                NextIndex = new ushort[Count];

                uint prevGood = 0;

                for (UInt32 i = 0; i < settings.BucketCount; i++)
                {
                    var bucketI = ReadBytes(offset + ObjIndexing.IndexMemberSize*i , ObjIndexing.IndexMemberSize).ReadUInt16();
                    Index[i] = bucketI;
                    if (bucketI < ObjIndexing.MaxIndex)
                    {
                        NextIndex[prevGood] = bucketI;
                        prevGood = bucketI;
                    }
                    else
                    {
                        NextIndex[prevGood] = (ushort)(settings.BucketCount - 1);
                    }
                }
            }

            // Console.WriteLine("Cur");
            // foreach (var keyValuePair in Index)
            // {
            //     Console.WriteLine($"{keyValuePair.Key} {keyValuePair.Value}");
            // }
            //
            // Console.WriteLine("Next");
            // foreach (var keyValuePair in NextIndex)
            // {
            //     Console.WriteLine($"{keyValuePair.Key} {keyValuePair.Value}");
            // }

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