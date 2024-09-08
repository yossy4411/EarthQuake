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
            if (mapLayer.Source is null || point.Z < mapLayer.MinZoom || point.Z > mapLayer.MaxZoom || !dict.TryGetValue(mapLayer.Source, out var layer)) return null;
            var features =
                layer.VectorTileFeatures.Where(v => mapLayer.IsVisible(new Dictionary<string, object>(v.Attributes)));
            return mapLayer.CreateFeature(features, point);
        }).Where(m => m is not null).OfType<VectorTileFeature>().ToArray();
    }

    private static SKColor ParseColor(string? value)
    {
        if (value is null) return SKColor.Empty;
        if (value.StartsWith('#')) return SKColor.TryParse(value, out var color) ? color : SKColor.Empty;

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
        var minZoom = jToken["minzoom"]?.ToObject<int>() ?? 0;
        var maxZoom = jToken["maxzoom"]?.ToObject<int>() ?? 22;
        switch (jToken["type"]?.ToObject<string>())
        {
            case "fill":
                var fillColorToken = paintToken!["fill-color"];
                var fillColor = ParseColor(fillColorToken?.ToObject<string>());
                return new VectorFillLayer(source, fillColor, vMapFilter) {MinZoom = minZoom, MaxZoom = maxZoom};
            case "line":
                var lineColorToken = paintToken!["line-color"];
                var lineColor = lineColorToken is null ? SKColor.Empty : ParseColor(lineColorToken.ToObject<string>());
                var lineWidthToken = paintToken["line-width"];
                List<(float, float)> lineWidth;
                if (lineWidthToken is JObject li)
                {
                    lineWidth = li["stops"]?.Select(x => x.ToObject<float[]>()).Select(x => (x![0], x[1])).ToList() ?? [];
                }
                else
                {
                    lineWidth = [(0, lineWidthToken?.ToObject<float>() ?? 1)];
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
                    PathEffect = pathEffect,
                    MinZoom = minZoom,
                    MaxZoom = maxZoom
                };
            case "symbol":
                var textSize = paintToken?["text-size"]?.ToObject<float>() ?? 12;
                var textColorToken = paintToken?["text-color"]?.ToObject<string>();
                var textColor = ParseColor(textColorToken);
                return new VectorSymbolLayer(source, textColor, textSize, vMapFilter)
                {
                    MinZoom = minZoom,
                    MaxZoom = maxZoom
                };
            default:
                return null;
        }
    }

    private static VectorMapFilter? GetFilter(JToken jToken)
    {
        if (jToken["filter"] is not JArray token) return null;
        if (token[1] is not JArray)
        {
            var key = token[1].ToObject<string>()!;
            switch (token[0].ToObject<string>())
            {
                case "==":
                {
                    var value = ParseValue(token[2]);
                    return dictionary => dictionary.TryGetValue(key, out var v) && CompareValue(value, v);
                }
                case "!=":
                {
                    var value = ParseValue(token[2]);
                    return dictionary => dictionary.TryGetValue(key, out var v) && !CompareValue(value, v);
                }
                case "in":
                {
                    var values = token.Skip(2).Select(ParseValue).ToArray();
                    return dictionary => dictionary.TryGetValue(key, out var v) && values.Any(x => CompareValue(x, v));
                }
            }

            return null;
        }
        List<VectorMapFilter> filterList = [];
        for (var i = 1; i < token.Count; i++)
        {
            var token1 = (JArray)token[i];
            var key = token1[1].ToObject<string>()!;
            switch (token1[0].ToObject<string>())
            {
                case "==":
                {
                    var value = ParseValue(token1[2]);
                    filterList.Add(dictionary => dictionary.TryGetValue(key, out var v) && CompareValue(value, v));
                    break;
                }
                case "!=":
                {
                    var value = ParseValue(token1[2]);
                    filterList.Add(dictionary => dictionary.TryGetValue(key, out var v) && !CompareValue(value, v));
                    break;
                }
                case "in":
                {
                    var values = token1.Skip(2).Select(ParseValue).ToArray();
                    filterList.Add(dictionary => dictionary.TryGetValue(key, out var v) && values.Any(x => CompareValue(x, v)));
                    break;
                }
                default:
                    continue;
            }
        }
        if (filterList.Count == 0) return _ => false;
        if (token[0].ToObject<string>() == "all")
            return dictionary => filterList.All(x => x(dictionary));
        return dictionary => filterList.Any(x => x(dictionary));
        
    }
    
    /// <summary>
    /// 値を展開します
    /// </summary>
    /// <param name="token">展開するトークン</param>
    /// <returns>(値, 型)</returns>
    private static (object?, int) ParseValue(JToken token)
    {
        return token.Type switch
        {
            JTokenType.String => (token.ToObject<string>(), 1),  // 文字列
            JTokenType.Integer => (token.ToObject<int>(), 2),    // 整数
            JTokenType.Float => (token.ToObject<float>(), 3),    // 浮動小数点数
            JTokenType.Boolean => (token.ToObject<bool>(), 4),   // 真偽値
            _ => (null, 0)
        };
    }
    
    private static bool CompareValue((object?, int) value, object target)
    {
        return value.Item2 switch
        {
            1 => target is string s && s == (string?)value.Item1,
            2 => target is long l && l == (int)value.Item1! || target is int i && i == (int)value.Item1!,
            3 => target is float f && Math.Abs(f - (float)value.Item1!) < 0.001f || target is double d && Math.Abs(d - (float)value.Item1!) < 0.001f,
            4 => target is bool b && b == (bool)value.Item1!,
            _ => false
        };
    }
}