#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace SickSharp
{
    public sealed class SickPath
    {
        public readonly SickPath? Parent;
        public readonly string Last;

        private SickPath(SickPath? parent, string last)
        {
            Parent = parent;
            Last = last;
        }

        public SickPath Append(string path)
        {
            return new SickPath(this, path);
        }

        public SickPath Append(int index)
        {
            return new SickPath(this, index.ToString());
        }

        public List<string> ToList()
        {
            var result = new Stack<string>();
            result.Push(Last);

            var current = Parent;
            while (current != null)
            {
                result.Push(current.Last);
                current = current.Parent;
            }

            return result.ToList();
        }

        public override string ToString()
        {
            return string.Join('.', ToList());
        }

        public static SickPath Single(string path)
        {
            return new SickPath(null, path);
        }

        public static SickPath FromQuery(string query)
        {
            SickPath current = null!;
            foreach (var part in SickReader.ParseQuery(query))
            {
                current = current == null! ? Single(part) : current.Append(part);
            }

            return current;
        }
    }
}