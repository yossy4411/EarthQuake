using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Core.EarthQuakes.P2PQuake
{
    /// <summary>
    /// P2P地震情報の緊急地震速報（コード556）を扱います
    /// </summary>
    public class PEewData() : PBasicData(556)
    {
        public bool Test { get; set; } = false;
        public EarthQuakeD? EarthQuake { get; set; }
        public IssueD Issue { get; set; } = new();
        public bool Cancelled { get; set; } = false;
        public AreaD[] Areas { get; set; } = []; 
        public class EarthQuakeD
        {
            public DateTime OriginTime { get; set; }
            public DateTime ArrivalTime { get; set; }
            public string? Condition { get; set; } // PLUM法の場合は「仮定震源要素」
            public Hypo Hypocenter { get; set; } = new();
            public class Hypo : P2PQuake.Hypo
            {
                public Hypo()
                {

                }
                public Hypo(string name, string reduceName, float latitude, float longitude, float depth, float magnitude)
                {
                    Name = name;
                    ReduceName = reduceName;
                    Latitude = latitude;
                    Longitude = longitude;
                    Depth = (int)depth;
                    Magnitude = magnitude;
                }
                public string ReduceName = string.Empty;
            }
        }
        public class IssueD
        {
            public DateTime Time { get; set; }
            public string EventId { get; set; } = string.Empty;
            public string Serial { get; set; } = string.Empty;
        }
        public class AreaD
        {
            public string Name { get; set; } = string.Empty;
            public string Pref { get; set; } = string.Empty;

        }
    }
}
