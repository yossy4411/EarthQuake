using EarthQuake.Core.EarthQuakes.P2PQuake.Client;

namespace EarthQuake.Core.EarthQuakes.P2PQuake;

/// <summary>
/// P2P地震情報の地震情報（コード551）を扱います
/// </summary>
public class PQuakeData() : PBasicData(551)
{
    public IssueD Issue { get; set; } = new();
    public EarthQuakeD EarthQuake { get; set; } = new();
    public ObsPoint[]? Points { get; set; }
    private bool _sorted;

    public override string ToString()
    {
        return $"""
                [{Issue.Type.ToScreenString()}]
                {EarthQuake.Time}頃発生
                最大震度: {EarthQuake.MaxScale.ToScreenString()}
                {(EarthQuake.Hypocenter is not null ? $"震源地: {EarthQuake.Hypocenter.Name ?? "なし"} (E{EarthQuake.Hypocenter.Longitude}, N{EarthQuake.Hypocenter.Latitude})" : "震央地名、震央要素なし")}
                {(EarthQuake.Hypocenter is not null ? $"M{EarthQuake.Hypocenter.Magnitude}, 深さ{EarthQuake.Hypocenter.Depth}km" : "震源要素なし")}
                {(Points is null ? "観測点データなし" : string.Join<ObsPoint>('\n', Points))}
                """;
    }

    public void SortPoints(IEnumerable<Station> stations)
    {
        if (Points is null) return;
        if (_sorted) return;
        _sorted = true;
        Points =
        [
            .. Points.OrderByDescending(x => (int)x.Scale)
                .ThenBy(x =>
                {
                    var station = stations.FirstOrDefault(v => v.Name is not null && v.Name.StartsWith(x.Addr));
                    return station is not null && EarthQuake.Hypocenter is not null
                        ? (station.Lat - EarthQuake.Hypocenter.Latitude) *
                          (station.Lat - EarthQuake.Hypocenter.Latitude) +
                          (station.Lon - EarthQuake.Hypocenter.Longitude) *
                          (station.Lon - EarthQuake.Hypocenter.Longitude)
                        : 1000;
                })
        ];
    }

    public static PQuakeData? TryParse(Response response)
    {
        if (response.Code != 551 || response.Body is null) return null;
        // 551 5 ABCDEFG:2005/03/27 12-34-56:12時34分頃,3,1,4,紀伊半島沖,ごく浅く,3.2,1,N12.3,E45.6,仙台管区気象台:-奈良県,+2,*下北山村,+1,*十津川村,*奈良川上村
        var info = response.Body[2].Split(',');
        EarthQuakeD earthQuake = new()
        {
            Time = DateTime.ParseExact(info[0], P2PClient.Format, null),
            MaxScale = P2PConverter.ParseScale(info[1]),
            DomesticTsunami = P2PConverter.ParseTsunami(info[2])
        };
        return new PQuakeData { EarthQuake = earthQuake }; // TODO: ちゃんと実装しなさい。
    }

    public class IssueD
    {
        public string? Source { get; set; }
        public DateTime Time { get; set; }
        public QuakeType Type { get; set; }
        public Fix Correct { get; set; }

        public enum QuakeType
        {
            ScalePrompt,
            Destination,
            ScaleAndDestination,
            DetailScale,
            Foreign,
            Other
        }

        public enum Fix
        {
            None,
            Unknown,
            ScaleOnly,
            DestinationOnly,
            ScaleAndDestination
        }
    }

    public class EarthQuakeD
    {
        public DateTime Time { get; set; }
        public Hypo? Hypocenter { get; set; }
        public Scale MaxScale { get; set; }
        public Tsunami DomesticTsunami { get; set; }
        public ForeTsunami ForeignTsunami { get; set; }
    }
}

public enum Tsunami
{
    None,
    Unknown,
    Checking,
    NonEffective,
    Watch,
    Warning,
}

public enum ForeTsunami
{
    None,
    Unknown,
    Checking,
    NonEffectiveNearby,
    WarningNearby,
    WarningPacific,
    WarningPacificWide,
    WarningIndian,
    WarningIndianWide,
    Potential
}

public class Hypo
{
    public string? Name { get; set; }
    public float Latitude { get; set; } = -200;
    public float Longitude { get; set; } = -200;
    public int Depth { get; set; } = -1;
    public float Magnitude { get; set; } = -1;
}

public class ObsPoint
{
    public string Pref { get; set; } = string.Empty;
    public string Addr { get; set; } = string.Empty;
    public bool IsArea { get; set; } = false;
    public Scale Scale { get; set; }

    public override string ToString()
    {
        return $"{Pref} - {Addr}: {Scale.ToScreenString()}";
    }
}