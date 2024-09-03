using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;

namespace EarthQuake.Map.Tiles;

public class VectorMapStyles
{
    public string? Name { get; init; }
    public IReadOnlyList<VectorTileMapLayer> Layers { get; init; } = [];
    
    private static readonly JsonSerializer Serializer = JsonSerializer.Create();
    
    /// <summary>
    /// GL Style JSONファイルを読み込む
    /// </summary>
    /// <param name="stream">読み込むストリーム</param>
    /// <returns>マップのスタイル</returns>
    public static VectorMapStyles LoadGLJson(StreamReader stream)
    {
        using var reader = new JsonTextReader(stream);
        var jObject = Serializer.Deserialize<JObject>(reader);
        if (jObject?["layers"] is not JArray layers) return new VectorMapStyles();
        var layersList = layers.Select(NewLayer).OfType<VectorTileMapLayer>().ToList();
        var name = jObject["name"]?.ToObject<string>();

        return new VectorMapStyles
        {
            Name = name,
            Layers = layersList
        };

    }

    private static VectorTileMapLayer? NewLayer(JToken jToken)
    {
        var vMapFilter = GetFilter(jToken);
        var source = jToken["source-layer"]?.ToObject<string>()!;
        var paintToken = jToken["paint"]!;
        switch (jToken["type"]?.ToObject<string>())
        {
            case "fill":
                var fillColorToken = paintToken["fill-color"];
                var fillOpacity = paintToken["fill-opacity"]?.ToObject<float>() ?? 1;
                var fillColor = fillColorToken is null
                    ? SKColor.Empty
                    : SKColor.Parse(fillColorToken.ToObject<string>()).WithAlpha((byte)(fillOpacity * 255));
                
                return new VectorFillLayer(source, fillColor, vMapFilter);
            case "line":
                var lineColorToken = paintToken["line-color"];
                var lineColor = lineColorToken is null ? SKColor.Empty : SKColor.Parse(lineColorToken.ToObject<string>());
                var lineWidth = paintToken["line-width"]?.ToObject<float>() ?? 1;
                var strokeCap = paintToken["line-cap"]?.ToObject<string>() switch
                {
                    "round" => SKStrokeCap.Round,
                    "square" => SKStrokeCap.Square,
                    _ => SKStrokeCap.Butt
                };
                var strokeJoin = paintToken["line-join"]?.ToObject<string>() switch
                {
                    "round" => SKStrokeJoin.Round,
                    "bevel" => SKStrokeJoin.Bevel,
                    _ => SKStrokeJoin.Miter
                };
                
                var dashArray = paintToken["line-dasharray"]?.ToObject<float[]>();
                var pathEffect = dashArray is null ? null : SKPathEffect.CreateDash(dashArray, 0);
                return new VectorLineLayer(source, lineColor, lineWidth, vMapFilter)
                {
                    StrokeCap = strokeCap,
                    StrokeJoin = strokeJoin,
                    PathEffect = pathEffect
                };
            case "symbol":
                var textField = paintToken["text-field"]?.ToObject<string>()!;
                var textSize = paintToken["text-size"]?.ToObject<float>() ?? 12;
                var textColorToken = paintToken["text-color"]?.ToObject<string>();
                var textColor = textColorToken is null
                    ? SKColor.Empty
                    : SKColor.Parse(textColorToken);
                return new VectorSymbolLayer(source, textField, textColor, textSize, vMapFilter);
            default:
                return null;
        }
    }

    private static VectorMapFilter? GetFilter(JToken jToken)
    {
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

            return vValues =>
            {
                Dictionary<string, string> dictionary;
                if (vValues is Dictionary<string, string> d)
                {
                    dictionary = d;
                }
                else
                {
                    dictionary = new Dictionary<string, string>(vValues);
                }
                if (dictionary.TryGetValue(key, out var v)) return false;
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

        var counter = filters[0].ToObject<string>() switch
        {
            "all" => 1,
            "any" => 2,
            _ => 0
        };
        if (filters[1] is not JArray) return null;
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

        return vValues =>
        {
            Dictionary<string, string> dictionary;
             if (vValues is Dictionary<string, string> d)
             {
                 dictionary = d;
             }
             else
             {
                 dictionary = new Dictionary<string, string>(vValues);
             }
            foreach (var (type, key, values) in filterList)
            {
                
                if (dictionary.TryGetValue(key, out var v)) return false;
                switch (counter)
                    {
                        case 1:　// ALL
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
                        case 2:  // ANY
                            switch (type)
                            {
                                case 1:
                                    if (v == values[0]) return true;
                                    break;
                                case 2:
                                    if (v != values[0]) return true;
                                    break;
                                case 3:
                                    if (values.Contains(v)) return true;
                                    break;
                            }

                            break;
                    }
            }

            return true;
        };
    }
}