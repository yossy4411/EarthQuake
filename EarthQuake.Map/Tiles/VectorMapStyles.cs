using System.Diagnostics;
using Mapbox.Vector.Tile;
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
    
    public VectorTileFeature[] ParsePaths(Stream stream, TilePoint point)
    {
        var layers = VectorTileParser.Parse(stream);
        var dict = new Dictionary<string, VectorTileLayer>(layers.Select(x => new KeyValuePair<string, VectorTileLayer>(x.Name, x)));
        return Layers.Select(mapLayer =>
        {
            if (mapLayer.Source is null || !dict.TryGetValue(mapLayer.Source, out var layer)) return null;
            var features = from v in layer.VectorTileFeatures
                where mapLayer.IsVisible(new Dictionary<string, object>(v.Attributes))
                select v;
            return mapLayer.CreateFeature(features, point);
        }).Where(m => m is not null).OfType<VectorTileFeature>().ToArray();
    }

    private static SKColor ParseColor(string? value)
    {
        switch (value)
        {
            case null:
                return SKColor.Empty;
            case ['#', _, _, _, _, _, _]:
                return SKColor.Parse(value);
        }

        if (value.StartsWith("rgba"))
        {
            // ex. rgba(40,20,100,0.8)
            var values = value[5..^1].Split(',');
            return new SKColor(byte.Parse(values[0]), byte.Parse(values[1]), byte.Parse(values[2]), (byte)(float.Parse(values[3]) * 255));
        }
        if (value.StartsWith("rgb"))
        {
            // ex. rgb(40,20,100)
            var values = value[4..^1].Split(',');
            return new SKColor(byte.Parse(values[0]), byte.Parse(values[1]), byte.Parse(values[2]));
        }
        Debug.WriteLine("Unknown color format: " + value);
        return SKColor.Empty;
    }
    
    private static VectorTileMapLayer? NewLayer(JToken jToken)
    {
        var vMapFilter = GetFilter(jToken);
        var source = jToken["source-layer"]?.ToObject<string>()!;
        var paintToken = jToken["paint"];
        switch (jToken["type"]?.ToObject<string>())
        {
            case "fill":
                var fillColorToken = paintToken!["fill-color"];
                var fillColor = ParseColor(fillColorToken?.ToObject<string>());
                return new VectorFillLayer(source, fillColor, vMapFilter);
            case "line":
                var lineColorToken = paintToken!["line-color"];
                var lineColor = lineColorToken is null ? SKColor.Empty : ParseColor(lineColorToken.ToObject<string>());
                var lineWidthToken = paintToken["line-width"];
                float lineWidth;
                if (lineWidthToken is JObject li)
                {
                    var start = li["stops"]?[0]![1]!.ToObject<float>() ?? 0;
                    lineWidth = start; // It is not so easy; f**king GL Style.
                    // TODO: 線形補間を実装する
                }
                else
                {
                    lineWidth = lineWidthToken?.ToObject<float>() ?? 1;
                }
               
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
                var pathEffect = dashArray is null || dashArray.Length % 2 != 0 ? null : SKPathEffect.CreateDash(dashArray, 0); // 多分偶数じゃないと機能しないんよね
                return new VectorLineLayer(source, lineColor, lineWidth, vMapFilter)
                {
                    StrokeCap = strokeCap,
                    StrokeJoin = strokeJoin,
                    PathEffect = pathEffect
                };
            case "symbol":
                var textSize = paintToken?["text-size"]?.ToObject<float>() ?? 12;
                var textColorToken = paintToken?["text-color"]?.ToObject<string>();
                var textColor = ParseColor(textColorToken);
                return new VectorSymbolLayer(source, textColor, textSize, vMapFilter);
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

            return dictionary =>
            {
                if (dictionary.TryGetValue(key, out var v) || v is not string s) return false;
                switch (type)
                {
                    case 1:
                        if (s != value[0]) return false;
                        break;
                    case 2:
                        if (s == value[0]) return false;
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

        return dictionary =>
        {
            foreach (var (type, key, values) in filterList)
            {
                
                if (dictionary.TryGetValue(key, out var v) || v is not string s) return false;
                switch (counter)
                    {
                        case 1:　// ALL
                            switch (type)
                            {
                                case 1:
                                    if (s != values[0]) return false;
                                    break;
                                case 2:
                                    if (s == values[0]) return false;
                                    break;
                                case 3:
                                    if (!values.Contains(s)) return false;
                                    break;
                            }

                            break;
                        case 2:  // ANY
                            switch (type)
                            {
                                case 1:
                                    if (s == values[0]) return true;
                                    break;
                                case 2:
                                    if (s != values[0]) return true;
                                    break;
                                case 3:
                                    if (values.Contains(s)) return true;
                                    break;
                            }

                            break;
                    }
            }

            return true;
        };
    }
}