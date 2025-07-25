using System.Collections.Generic;
using Data;

namespace Building
{
    public partial class BuildSystem
    {
        public static class ConstructionConfig
        {
            public static readonly Dictionary<ConstructionType, ConstructionProperties> Data = new()
            {
                { ConstructionType.House,      new ConstructionProperties(ConstructionType.House,      2, 10, 2) },
                { ConstructionType.SegmentWall,new ConstructionProperties(ConstructionType.SegmentWall,1, 5,  0)  },
                { ConstructionType.Wall,       new ConstructionProperties(ConstructionType.Wall,       3, 15, 0)  },
                { ConstructionType.Tower,      new ConstructionProperties(ConstructionType.Tower,      4, 20, 0)  }
            }
            ;
        }
    }
}