using EarthQuake.Core;
using SkiaSharp;
using System.Diagnostics;
using EarthQuake.Core.TopoJson;

namespace EarthQuake.Map.Layers;

public class CountriesLayer(WorldPolygonSet world) : MapLayer
{
    private SKPoint[][]? data = world.Polygons;
    internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
    {
        using SKPaint paint = new();
        paint.Color = SKColors.Green;
        foreach (var polygon in polygons)
        {
            canvas.DrawVertices(polygon, SKBlendMode.Clear, paint);
        }
    }
    private SKVertices[] polygons = [];
    private protected override void Initialize()
    {
        var sw = Stopwatch.StartNew();
        if (data is not null)
        {
            polygons = new SKVertices[data.Length];
            for (var i = 0; i < data.Length; i++)
            {
                polygons[i] = SKVertices.CreateCopy(SKVertexMode.Triangles, data[i].Select(GeomTransform.Translate).ToArray(),
                    null);
            }
        }
        Debug.WriteLine($"World: {sw.ElapsedMilliseconds}ms");

        data = null;
    }
}