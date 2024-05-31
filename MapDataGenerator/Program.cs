using EarthQuake.Core;
using EarthQuake.Core.TopoJson;
using LibTessDotNet;
using MessagePack;
using Newtonsoft.Json;
using SkiaSharp;
using System.Diagnostics;
using TIndex = EarthQuake.Core.TopoJson.Index;

static bool contains(Feature feature, int v)
{
    foreach (int[][] a1 in feature.Arcs)
    {
        foreach (int[] a2 in a1)
        {
            foreach (int a3 in a2)
            {
                if ((a3 < 0 ? -a3 - 1 : a3) == v) return true;
            }
        }
    }
    return false;
}

TopoJson? topo = JsonConvert.DeserializeObject<TopoJson>(File.ReadAllText("japan.json"));
if (topo == null) return;
CalculatedPolygons? load(string layerName)
{
    var Data = topo.GetLayer(layerName);
    var geo = new GeomTransform();


    if (Data is not null && Data.Geometries is not null)
    {
        Polygon[] parent = new Polygon[Data.Geometries.Length];
        for (int i = 0; i < Data.Geometries.Length; i++)
        {
            Feature? feature = Data.Geometries[i];
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            Point[][] points1 = new Point[6][];
            for (int i2 = 0; i2 < 6; i2++)
            {
                Data.Simplify = i2 switch
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

                        Data.AddVertex(tess, polygon[0], geo, ref minX, ref minY, ref maxX, ref maxY);
                    }
                }
                tess.Tessellate(WindingRule.Positive); // テッセレーションを通す。
                Point[] points = new Point[tess.ElementCount * 3];
                for (int j = 0; j < points.Length; j++)
                {
                    points[j] = new(tess.Vertices[tess.Elements[j]].Position.X, tess.Vertices[tess.Elements[j]].Position.Y);
                }
                points1[i2] = points;
                
            }
            parent[i] = new Polygon(points1, minX, maxX, minY, maxY);
        }
        string[] names = new string[Data.Geometries.Length];
        for (int i = 0; i < Data.Geometries.Length; i++) names[i] = Data.Geometries[i].Properties!.Name!;

        return new CalculatedPolygons(names, parent);
    } 
    else
    {
        return null;
    }
}

CalculatedBorders generateBorders() {
    string[] layerNames = new string[topo.Objects.Count];
    var arcs = topo.Arcs;
    Border[] borders = new Border[arcs.Length];
    
    for (int e = 0; e < arcs.Length; e++)
    {
        List<TIndex> indice = [];
        int ij = 0;
        foreach ((string layerName, Layer layer) in topo.Objects.ToList().OrderByDescending(x => x.Key == "city"))
        {
            layerNames[ij] = layerName;

            for (int j = 0; j < layer.Geometries.Length; j++)
            {
                Feature feature = layer.Geometries[j];
                if (contains(feature, e))
                    indice.Add(new TIndex(ij, j));
            }
            ij++;
        }
        
        var _transform = topo.Transform;

        var coords = arcs[e];
        Point[][] points1 = new Point[6][];
        for (int s = 0; s < 6; s++)
        {
            double Simplify = s switch
            {
                0 => 0,
                1 => 0.5,
                _ => (s - 1) * s
            };
            List<Point> points = [];
            int x = coords[0][0], y = coords[0][1];
            var _point = _transform.ToPoint(x, y);

            points.Add(_point);
            
            for (int i = 1; i < coords.Length; i++)
            {
                x += coords[i][0];
                y += coords[i][1];
                var point = _transform.ToPoint(x, y);
                if (Simplify == 0 || SKPoint.Distance(_point, point) * 50 >= Simplify || i == coords.Length - 1)
                {
                    points.Add(point);
                    _point = point;
                }
            }
            points1[s] = [.. points];
        }
        borders[e] = new Border([.. indice], points1);
    }
    return new CalculatedBorders(layerNames, borders);
}

var a = load("info");
var b = load("city");

var border = generateBorders();

if (a is null || b is null)
{
    Console.WriteLine("名前がnullのためシリアライズ出来ませんでした。");
    return;
}

MessagePackSerializerOptions lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
PolygonsSet result = new(a, b, border);
Stopwatch sw = Stopwatch.StartNew();
byte[] bytes = MessagePackSerializer.Serialize(result, lz4Options);
long serialize, deserialize;
sw.Stop();
Console.WriteLine($"シリアライズには{serialize = sw.ElapsedMilliseconds}msかかり、{bytes.LongLength / 1024}KBのデータになりました。（圧縮済み）");
Console.WriteLine("デシリアライズのテストをします。");
sw.Restart();
_ = MessagePackSerializer.Deserialize<PolygonsSet>(bytes, lz4Options);
sw.Stop();
Console.WriteLine($"デシリアライズには{deserialize = sw.ElapsedMilliseconds}msかかりました。({deserialize * 100.0 / serialize:0.00}%)");

File.WriteAllBytes(@"japan.mpk.lz4", bytes);