﻿using EarthQuake.Core;
using SkiaSharp;
using System.Diagnostics;
using EarthQuake.Core.TopoJson;

namespace EarthQuake.Map.Layers;

public class CountriesLayer(WorldPolygonSet world) : MapLayer
{
    private SKPoint[]? data = world.Polygons;

    public override void Render(SKCanvas canvas, float scale, SKRect bounds)
    {
        using SKPaint paint = new();
        paint.Color = SKColors.Green;

        canvas.DrawVertices(polygons, SKBlendMode.SrcOver, paint);
    }

    private SKVertices? polygons;

    private protected override void Initialize()
    {
        var sw = Stopwatch.StartNew();
        if (data is not null)
        {
            polygons = SKVertices.CreateCopy(SKVertexMode.Triangles, data.Select(GeomTransform.Translate).ToArray(),
                null);
        }

        Debug.WriteLine($"World: {sw.ElapsedMilliseconds}ms");

        data = null;
    }
}