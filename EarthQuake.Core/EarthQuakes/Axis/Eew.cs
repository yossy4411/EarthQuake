namespace EarthQuake.Core.EarthQuakes.Axis;

/// <summary>
/// AXISの緊急地震速報
/// </summary>
/// <param name="EventID">ID</param>
/// <param name="Intensity">最大震度</param>
/// <param name="Hypocenter">震源情報</param>
public class Eew(string EventID, string Intensity, Hypocenter Hypocenter)
{
    public DateTime OriginDateTime { get; set; }
    public DateTime ReportDateTime { get; set; }
    public long EventID { get; set; } = long.Parse(EventID);
    public int Serial { get; set; }
    public Hypocenter Hypocenter { get; set; } = Hypocenter;
    public Scale Intensity { get; set; } = Converter.FromString(Intensity);
}

/// <summary>
/// AXISの震源情報
/// </summary>
/// <param name="Coordinate"></param>
/// <param name="Depth"></param>
/// <param name="Code"></param>
/// <param name="Name"></param>
public class Hypocenter(float[] Coordinate, string Depth, int Code = -1, string Name = "不明")
{
    public int Code { get; init; } = Code;
    public string Name { get; init; } = Name;
    public float Longitude { get; init; } = Coordinate[0];
    public float Latitude { get; init; } = Coordinate[1];
    public string Depth { get; init; } = Depth;
}