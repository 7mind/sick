using System.Collections.Generic;
using SickSharp.Format.Tables;

namespace SickSharp
{
    internal record Header(int Version, int TableCount, List<int> Offsets, ObjIndexing Settings);
}