using System.Collections.Generic;

namespace SickSharp.Format
{
    public record Header(int Version, int TableCount, List<int> Offsets);
}