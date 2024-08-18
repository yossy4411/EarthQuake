using EarthQuake.Core;
using EarthQuake.Core.TopoJson;
using LibTessDotNet;
using MessagePack;
using Newtonsoft.Json;
using SkiaSharp;
using System.Diagnostics;
using TIndex = EarthQuake.Core.TopoJson.Index;

var topo = JsonConvert.DeserializeObject<TopoJson>(File.ReadAllText("japan.json"));
if (topo == null) return;

var a = Load("info");
var b = Load("city");

var border = GenerateBorders();

if (a is null || b is null)
{
    Console.WriteLine("名前がnullのためシリアライズ出来ませんでした。");
    return;
}

var lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
PolygonsSet result = new(a, b, border);
var sw = Stopwatch.StartNew();
var bytes = MessagePackSerializer.Serialize(result, lz4Options);
long serialize, deserialize;
sw.Stop();
Console.WriteLine($"シリアライズには{serialize = sw.ElapsedMilliseconds}msかかり、{bytes.LongLength / 1024}KBのデータになりました。（圧縮済み）");
Console.WriteLine("デシリアライズのテストをします。");
sw.Restart();
_ = MessagePackSerializer.Deserialize<PolygonsSet>(bytes, lz4Options);
sw.Stop();
Console.WriteLine($"デシリアライズには{deserialize = sw.ElapsedMilliseconds}msかかりました。({deserialize * 100.0 / serialize :0.00}%)");

File.WriteAllBytes("japan.mpk.lz4", bytes);

// 保存したファイルの完全パスを表示
Console.WriteLine(Path.GetFullPath("japan.mpk.lz4"));

return;

CalculatedBorders GenerateBorders() {
    var layerNames = new string[topo.Objects.Count];
    var arcs = topo.Arcs;
    var borders = new Border[arcs.Length];
    
    for (var e = 0; e < arcs.Length; e++)
    {
        List<TIndex> indices = [];
        var ij = 0;
        foreach (var (layerName, layer) in topo.Objects.ToList().OrderByDescending(x => x.Key == "city"))
        {
            layerNames[ij] = layerName;

            for (var j = 0; j < layer.Geometries.Length; j++)
            {
                var feature = layer.Geometries[j];
                if (Contains(feature, e))
                    indices.Add(new TIndex(ij, j));
            }
            ij++;
        }
        
        var transform = topo.Transform;

        var coords = arcs[e];
        var points1 = new Point[6][];
        for (var s = 0; s < 6; s++)
        {
            var simplify = s switch
            {
                0 => 0,
                1 => 0.5,
                _ => (s - 1) * s
            };
            List<Point> points = [];
            int x = coords[0][0], y = coords[0][1];
            var pointG = transform.ToPoint(x, y);

            points.Add(pointG);
            
            for (var i = 1; i < coords.Length; i++)
            {
                x += coords[i][0];
                y += coords[i][1];
                var point = transform.ToPoint(x, y);
                if (simplify == 0 || SKPoint.Distance(pointG, point) * 50 >= simplify || i == coords.Length - 1)
                {
                    points.Add(point);
                    pointG = point;
                }
            }
            points1[s] = [.. points];
        }
        borders[e] = new Border([.. indices], points1);
    }
    return new CalculatedBorders(layerNames, borders);
}

/*
 * Loadとか言っておきながら、実際にはTopojsonのデータを読み込んで、もうポリゴンまで計算している。
 */
CalculatedPolygons? Load(string layerName)
{
    var data = topo.GetLayer(layerName);
    var geo = new GeomTransform();


    if (data?.Geometries is not null)
    {
        var parent = new Polygon[data.Geometries.Length];
        for (var i = 0; i < data.Geometries.Length; i++)
        {
            var feature = data.Geometries[i];
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            var points1 = new Point[6][];
            for (var i2 = 0; i2 < 6; i2++)
            {
                data.Simplify = i2 switch
                {
                    0 => 0,
                    1 => 0.5,
                    _ => (i2 - 1) * i2
                };

                Tess tess = new();
                if (feature.Arcs is not null)
                {
                    foreach (var polygon in feature.Arcs)
                    {

                        data.AddVertex(tess, polygon[0], geo, ref minX, ref minY, ref maxX, ref maxY);
                    }
                }
                tess.Tessellate(WindingRule.Positive); // テッセレーションを通す。
                var points = new Point[tess.ElementCount * 3];
                for (var j = 0; j < points.Length; j++)
                {
                    points[j] = new Point(tess.Vertices[tess.Elements[j]].Position.X, tess.Vertices[tess.Elements[j]].Position.Y);
                }
                points1[i2] = points;
                
            }
            parent[i] = new Polygon(points1, minX, maxX, minY, maxY);
        }
        var names = new string[data.Geometries.Length];
        for (var i = 0; i < data.Geometries.Length; i++) names[i] = data.Geometries[i].Properties!.Name!;

        return new CalculatedPolygons(names, parent);
    } 
    else
    {
        return null;
    }
}

static bool Contains(Feature feature, int v)
{
    return (from a1 in feature.Arcs from a2 in a1 from a3 in a2 select a3).Any(a3 => (a3 < 0 ? -a3 - 1 : a3) == v);
}
