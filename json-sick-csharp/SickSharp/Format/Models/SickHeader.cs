using System.Collections.Generic;
using SickSharp.Format.Tables;

namespace SickSharp
{
    internal record Header(int Version, int TableCount, IReadOnlyList<int> Offsets, ObjIndexing Settings);
}