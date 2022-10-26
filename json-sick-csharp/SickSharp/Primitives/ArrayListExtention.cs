using System.Collections.Generic;
using System.Linq;

namespace SickSharp.Primitives
{
    public static class ArrayListExtention
    {
        public static T[] Concatenate<T>(this IEnumerable<T[]> arrays)
        {
            var sz = arrays.Select(a => a.Length).Sum();
            T[] result = new T[sz];
            var nextOffset = 0;
            foreach (var array in arrays)
            {
                array.CopyTo(result, nextOffset);
                nextOffset += array.Length;
            }
            return result;
        }
    }
}