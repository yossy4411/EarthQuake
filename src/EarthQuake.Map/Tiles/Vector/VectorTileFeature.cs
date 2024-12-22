using System.Diagnostics;
using EarthQuake.Core;
using LibTessDotNet;
using SkiaSharp;
using VectorTiles.Styles;
using VectorTiles.Values;
using MVectorTileFeature = VectorTiles.Mvt.MapboxTile.Layer.Feature;

namespace EarthQuake.Map.Tiles.Vector;

public abstract class VectorTileFeature(VectorMapStyleLayer layer, Dictionary<string, IConstValue?> tags) : IDisposable
{
    internal VectorMapStyleLayer? Layer { get; private set; } = layer;

    public Dictionary<string, IConstValue?>? Tags { get; set; } = tags;

    protected virtual void Dispose(bool disposing)
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

public class VectorFillFeature : VectorTileFeature
{
    
    public SKVertices? Vertices { get; init; }

    public VectorFillFeature(MVectorTileFeature feature, VectorMapStyleLayer layer, Dictionary<string, IConstValue?> tags) : base(layer, tags)
    {
        var tess = new Tess();
        if (feature.Geometries.Count <= 0) return;
        foreach (var contourVertices in feature.Geometries.Select(coordinates =>
                     coordinates.Points.Select(v => GeomTransform.Translate(v.Lon, v.Lat))
                         .Select(pos => new ContourVertex(new Vec3 { X = pos.X, Y = pos.Y, Z = 0 })).ToList()))
        {
            // 生成された頂点を tess に追加
            tess.AddContour(contourVertices);
        }
        tess.Tessellate(WindingRule.Positive);
        
        var points = new SKPoint[tess.ElementCount * 3];
        for (var j = 0; j < points.Length; j++)
        { 
            points[j] = new SKPoint(tess.Vertices[tess.Elements[j]].Position.X, 
                tess.Vertices[tess.Elements[j]].Position.Y);
        }
        Vertices = SKVertices.CreateCopy(SKVertexMode.Triangles, points, null);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Vertices?.Dispose();
    }
}

public class VectorLineFeature : VectorTileFeature
{
    public SKPath Path { get; } = new();

    public VectorLineFeature(MVectorTileFeature feature, VectorMapStyleLayer layer, Dictionary<string, IConstValue?> tags) : base(layer,tags)
    {
        foreach (var points in feature.Geometries.Select(featureGeometry =>
                     featureGeometry.Points.Select(a => GeomTransform.Translate(a.Lon, a.Lat)).ToArray()))
        {
            Path.AddPoly(points, false);
        }
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Path.Dispose();
    }
}

public class VectorSymbolFeature : VectorTileFeature
{
    public string? Text { get; }
    public SKPoint Point { get; }
    public VectorSymbolFeature(MVectorTileFeature feature, VectorMapStyleLayer layer, Dictionary<string, IConstValue?> tags) : base(layer, tags)
    {
        var field = (Layer as VectorSymbolStyleLayer)?.TextField;
        if (field is null)
        {
            Text = null;
            return;
        }
        var coord = feature.Geometries[0].Points[0];
        Point = GeomTransform.Translate(coord.Lon, coord.Lat);
        var text = feature.Tags.GetValueOrDefault(field)?.ToString();
        Text = text;
    }
}