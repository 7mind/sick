using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SickSharp.Primitives
{
    public static class ByteArrayCollectionExtensions
    {
        public static List<int> ComputeOffsets(this List<byte[]> collection, Int32 initial)
        {
                
            var res = new List<int>() {initial};
            var counts = collection.Select(a => a.Length).ToList();
            for (int i = 0; i < counts.Count(); i++)
            {
                res.Add(res.Last() + counts[i]);
            }
            
            res.RemoveAt(res.Count -1);
            Debug.Assert(res.Count == collection.Count);
            return res;
        }
    }
}