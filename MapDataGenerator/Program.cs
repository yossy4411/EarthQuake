using EarthQuake.Core;
using EarthQuake.Core.TopoJson;
using LibTessDotNet;
using MessagePack;
using Newtonsoft.Json;
using System.Diagnostics;



TopoJson? topo = JsonConvert.DeserializeObject<TopoJson>(File.ReadAllText("japan.json"));

void load(string layerName, out string[]? names, out Polygon[][] dict)
{
    var Data = topo?.GetLayer(layerName);
    var geo = new GeomTransform();

    dict = new Polygon[6][];
    if (Data is not null && Data.Geometries is not null)
    {
        for (int i2 = 0; i2 < 6; i2++)
        {
            Data.Simplify = i2 switch
            {
                0 => 0,
                1 => 0.5,
                _ => (i2 - 1) * i2
            };
            Polygon[] parent = new Polygon[Data.Geometries.Length];
            for (int i = 0; i < Data.Geometries.Length; i++)
            {
                float minX = float.MaxValue;
                float maxX = float.MinValue;
                float minY = float.MaxValue;
                float maxY = float.MinValue;
                Feature? feature = Data.Geometries[i];
                Tess tess = new();
                if (feature.Arcs is not null)
                {
                    foreach (var polygon in feature.Arcs)
                    {

                        Data.AddVertex(tess, polygon[0], geo, ref minX, ref minY, ref maxX, ref maxY);
                    }
                }
                tess.Tessellate(WindingRule.Positive);
                Point[] points = new Point[tess.ElementCount * 3];
                for (int j = 0; j < points.Length; j++)
                {
                    points[j] = new(tess.Vertices[tess.Elements[j]].Position.X, tess.Vertices[tess.Elements[j]].Position.Y);
                }
                parent[i] = new Polygon(points, minX, maxX, minY, maxY);
            }
            dict[i2] =  parent;
        }
        names = new string[Data.Geometries.Length];
        for (int i = 0; i < Data.Geometries.Length; i++) names[i] = Data.Geometries[i].Properties!.Name!;
    } 
    else
    {
        names = null;
    }
}
load("info", out var namea, out var dicta);
load("city", out var nameb, out var dictb);

if (namea is null || nameb is null)
{
    Console.WriteLine("名前がnullのためシリアライズ出来ませんでした。");
    return;
}

MessagePackSerializerOptions lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
CalculatedPolygons a = new(namea, dicta);
CalculatedPolygons b = new(nameb, dictb);
Dictionary<string, CalculatedPolygons> result = new()
{
    { "info", a },
    { "city", b }
};
Stopwatch sw = Stopwatch.StartNew();
byte[] bytes = MessagePackSerializer.Serialize(result, lz4Options);
long serialize, deserialize;
sw.Stop();
Console.WriteLine($"シリアライズには{serialize = sw.ElapsedMilliseconds}msかかり、{bytes.LongLength / 1024}KBのデータになりました。（圧縮済み）");
Console.WriteLine("デシリアライズのテストをします。");
sw.Restart();
_ = MessagePackSerializer.Deserialize<Dictionary<string, CalculatedPolygons>>(bytes, lz4Options);
sw.Stop();
Console.WriteLine($"デシリアライズには{deserialize = sw.ElapsedMilliseconds}msかかりました。({deserialize * 100.0 / serialize:0.00}%)");

File.WriteAllBytes(@"result.mpk.lz4", bytes);