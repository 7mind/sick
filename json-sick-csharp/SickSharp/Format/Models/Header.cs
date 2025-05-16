using System.Collections.Generic;
using SickSharp.Format.Tables;

namespace SickSharp.Format
{
    public record Header(int Version, int TableCount, List<int> Offsets, ObjIndexing Settings);
}