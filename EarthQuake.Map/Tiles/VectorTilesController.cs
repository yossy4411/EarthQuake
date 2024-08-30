using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;

namespace EarthQuake.Map.Tiles;

public class VectorTilesController(string url, VectorMapStyles styles) : MapTilesController<VectorTile>(url)
{
    private protected override async Task<VectorTile> GetTile(HttpClient client, SKPoint point, TilePoint point1)
    {
        var response = await client.GetAsync(GenerateUrl(Url, point1.X, point1.Y, point1.Z));
        if (!response.IsSuccessStatusCode) return new VectorTile(point, 0, null);
        var network = await response.Content.ReadAsByteArrayAsync();
        return new VectorTile(point, MathF.Pow(2, point1.Z), styles.ParsePaths(network));
    }
}
public record VectorTile(SKPoint LeftTop, float Zoom, SKPath[]? Vertices);

public class VectorMapStyles
{
    public string? Name { get; init; } = null;
    public VectorMapLayer[] Layers { get; init; } = [];
    private static readonly JsonSerializer Serializer = JsonSerializer.Create();
    
    /// <summary>
    /// PBFデータをパースしてパスを生成する
    /// </summary>
    /// <param name="data">受信したベクトルタイルのデータ</param>
    /// <returns>解析されたパス</returns>
    public SKPath[] ParsePaths(byte[] data)
    {
        // TODO: データを解析してスタイルに合うようにパスを生成する
    }
    
    /// <summary>
    /// GL Style JSONファイルを読み込む
    /// </summary>
    /// <param name="stream">読み込むストリーム</param>
    /// <returns>マップのスタイル</returns>
    public static VectorMapStyles Load(StreamReader stream)
    {
        using var reader = new JsonTextReader(stream);
        var jObject = Serializer.Deserialize<JObject>(reader);
        if (jObject?["layers"] is not JArray layers) return new VectorMapStyles();
        var vectorLayers = new VectorMapLayer[layers.Count];

        for (var i1 = 0; i1 < layers.Count; i1++)
        {
            var jToken = layers[i1];
            VectorMapFilter vMapFilter;
            var token = jToken["filter"] as JArray;
            if (token?[1] is not JArray filters)
            {
                var type = token![0].ToObject<string>() switch
                {
                    "==" => 1,
                    "!=" => 2,
                    "in" => 3,
                    _ => 0
                };
                var key = token[1].ToObject<string>()!;
                var value = token.Where((_, j) => 1 < j).Select(x => x.ToObject<string>()!).ToArray();

                vMapFilter = vValues =>
                {
                    if (!vValues.TryGetValue(key, out var v)) return false;
                    switch (type)
                    {
                        case 1:
                            if (v != value[0]) return false;
                            break;
                        case 2:
                            if (v == value[0]) return false;
                            break;
                        case 3:
                            if (!value.Contains(v)) return false;
                            break;
                    }

                    return true;
                };
            }
            else
            {
                var counter = filters[0].ToObject<string>() switch
                {
                    "all" => 1,
                    "any" => 2,
                    _ => 0
                };
                if (filters[1] is not JArray) continue;
                List<(int, string, string[])> filterList = [];
                for (var i = 1; i < filters.Count; i++)
                {
                    var filter = (filters[i] as JArray)!;
                    var type = filter[0].ToObject<string>() switch
                    {
                        "==" => 1,
                        "!=" => 2,
                        "in" => 3,
                        _ => 0
                    };
                    var key = filter[1].ToObject<string>()!;
                    var value = filter.Where((_, j) => 1 < j).Select(x => x.ToObject<string>()!).ToArray();
                    filterList.Add((type, key, value));
                }

                vMapFilter = vValues =>
                {
                    foreach (var (type, key, values) in filterList)
                    {
                        if (vValues.TryGetValue(key, out var v))
                        {
                            switch (counter)
                            {
                                case 1:
                                    switch (type)
                                    {
                                        case 1:
                                            if (v != values[0]) return false;
                                            break;
                                        case 2:
                                            if (v == values[0]) return false;
                                            break;
                                        case 3:
                                            if (!values.Contains(v)) return false;
                                            break;
                                    }

                                    break;
                                case 2:
                                    throw new NotImplementedException();
                            }
                        }
                    }

                    return true;
                };
            }

            var layerType = jToken["type"]?.ToObject<string>() switch
            {
                "fill" => VectorMapLayerType.Fill,
                "line" => VectorMapLayerType.Line,
                "symbol" => VectorMapLayerType.Symbol,
                "circle" => VectorMapLayerType.Circle,
                _ => VectorMapLayerType.Fill
            };
            var source = jToken["source-layer"]?.ToObject<string>()!;
            var paintToken = jToken["paint"];
            var jToken1 = paintToken!["fill-color"];
            var fillColor = jToken1 is null ? SKColor.Empty : SKColor.Parse(jToken1.ToObject<string>());
            var token1 = paintToken["line-color"];
            var lineColor = token1 is null ? SKColor.Empty : SKColor.Parse(token1.ToObject<string>());
            var token2 = paintToken["line-width"];
            var lineWidth = token2?.ToObject<float>() ?? 0;
            vectorLayers[i1] = new VectorMapLayer(layerType, source, fillColor, lineColor, lineWidth, vMapFilter);
        }

        return new VectorMapStyles
        {
            Layers = vectorLayers
        };

    }
}

public delegate bool VectorMapFilter(Dictionary<string, string> values);

public class VectorMapLayer(
    VectorMapLayerType type,
    string source,
    SKColor fillColor,
    SKColor lineColor = default,
    float lineWidth = default,
    VectorMapFilter? filter = null)
{
    public VectorMapLayerType Type { get; init; } = type;
    public string Source { get; init; } = source;
    public SKColor FillColor { get; init; } = fillColor;
    public SKColor LineColor { get; init; } = lineColor;
    public float LineWidth { get; init; } = lineWidth;
    public VectorMapFilter? Filter { get; init; } = filter;
}

public enum VectorMapLayerType
{
    Fill,   // 塗りつぶし
    Line,   // 線
    Symbol, // テキスト
    Circle  // 円
}