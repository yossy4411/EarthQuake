using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Core.EarthQuakes.Axis
{

    public class Eew(string EventID, string Intensity)
    {
        public DateTime OriginDateTime { get; set; }
        public DateTime ReportDateTime { get; set; }
        public long EventID { get; set; } = long.Parse(EventID);
        public int Serial { get; set; }
        public Hypocenter Hypocenter { get; set; } = new([], "ごく浅い");
        public Scale Intensity { get; set; } = Converter.FromString(Intensity);

    }
    public class Hypocenter(float[] Coordinate, string Depth, int Code = -1, string Name = "不明")
    {
        public int Code { get; init; } = Code;
        public string Name { get; init; } = Name;
        public float Longitude { get; init; } = Coordinate[0];
        public float Latitude { get; init; } = Coordinate[1];
        public int Depth { get; init; } = Depth == "ごく浅い" ? 0 : int.Parse(Depth.Replace("km", ""));
    }
}
