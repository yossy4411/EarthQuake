using System.Diagnostics;
using EarthQuake.Core;
using LibTessDotNet;
using SkiaSharp;
using VectorTiles.Styles;
using VectorTiles.Values;
using MVectorTileFeature = VectorTiles.Mvt.MapboxTile.Layer.Feature;

namespace EarthQuake.Map.Tiles.Vector;

public abstract class VectorTileFeature(VectorMapStyleLayer layer, Dictionary<string, IConstValue?> tags)
{
    internal VectorMapStyleLayer? Layer { get; private set; } = layer;

    public Dictionary<string, IConstValue?>? Tags { get; set; } = tags;
    
}

public class VectorFillFeature : VectorTileFeature
{
    public SKPoint[]? Geometry { get; private set; }

    public VectorFillFeature(MVectorTileFeature feature, VectorMapStyleLayer layer, Dictionary<string, IConstValue?> tags) : base(layer, tags)
    {
        var tess = new Tess();
        if (feature.Geometries.Count <= 0) return;
        foreach (var coordinates in feature.Geometries)
        {
            var contourVertices = new List<ContourVertex>(coordinates.Points.Count); // 結果を格納するリスト
            contourVertices.AddRange(coordinates.Points.Select(v => GeomTransform.Translate(v.Lon, v.Lat)).Select(pos => new ContourVertex(new Vec3 { X = pos.X, Y = pos.Y, Z = 0 })));
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

        Geometry = points;
    }
}

public class VectorLineFeature : VectorTileFeature
{
    public SKPoint[][] Geometry { get; private set; }

    public VectorLineFeature(MVectorTileFeature feature, VectorMapStyleLayer layer, Dictionary<string, IConstValue?> tags) : base(layer,tags)
    {
        Geometry = feature.Geometries.Select(p => p.Points.Select(a => GeomTransform.Translate(a.Lon, a.Lat)).ToArray())
            .ToArray();
    }
}

public class VectorSymbolFeature : VectorTileFeature
{
    public string? Text { get; }
    public SKPoint[] Point { get; }
    public VectorSymbolFeature(MVectorTileFeature feature, VectorMapStyleLayer layer, Dictionary<string, IConstValue?> tags) : base(layer, tags)
    {
        var field = (Layer as VectorSymbolStyleLayer)?.TextField;
        if (field is null)
        {
            Text = null;
            return;
        }
        Point = feature.Geometries[0].Points.Select(coord => GeomTransform.Translate(coord.Lon, coord.Lat)).ToArray();
        Text = feature.Tags.GetValueOrDefault(field)?.ToString();
    }
}