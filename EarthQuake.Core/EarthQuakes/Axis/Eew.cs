using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Core.EarthQuakes.Axis
{

    public class Eew(string EventID, string Intensity, Hypocenter Hypocenter)
    {
        public DateTime OriginDateTime { get; set; }
        public DateTime ReportDateTime { get; set; }
        public long EventID { get; set; } = long.Parse(EventID);
        public int Serial { get; set; }
        public Hypocenter Hypocenter { get; set; } = Hypocenter;
        public Scale Intensity { get; set; } = Converter.FromString(Intensity);

    }
    public class Hypocenter(float[] Coordinate, string Depth, int Code = -1, string Name = "不明")
    {
        public int Code { get; init; } = Code;
        public string Name { get; init; } = Name;
        public float Longitude { get; init; } = Coordinate[0];
        public float Latitude { get; init; } = Coordinate[1];
        public string Depth { get; init; } = Depth;
    }
}
