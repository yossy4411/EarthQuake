﻿using EarthQuake.Core;
using LibTessDotNet;
using Mapbox.Vector.Tile;
using SkiaSharp;
using MVectorTileFeature = Mapbox.Vector.Tile.VectorTileFeature;

namespace EarthQuake.Map.Tiles.Vector;

public abstract class VectorTileFeature : IDisposable
{
    public virtual SKObject? Geometry => null;
    public VectorTileMapLayer Layer { get; init; } = null!;

    public virtual void Dispose()
    {
        if (Geometry is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    private protected static uint GetFactor(TilePoint point)
    {
        return point.Z < 8 ? 16384u : 4096u; // 地理院地図の場合
        // return 4096u; // Mapbox の場合
    }
}

public class VectorFillFeature : VectorTileFeature
{
    public override SKVertices? Geometry { get; }

    public VectorFillFeature(IEnumerable<MVectorTileFeature> features, TilePoint point)
    {
        var tess = new Tess();
        foreach (var feature in features)
        {
            if (feature.Geometry.Count <= 0) return;
            foreach (var coordinates in feature.Geometry)
            {
                var contourVertices = new List<ContourVertex>(coordinates.Count); // 結果を格納するリスト
                foreach (var v in coordinates)
                {
                    // coord を計算
                    var coord = v.ToPosition(point.X, point.Y, point.Z, GetFactor(point));
                    // pos を計算
                    var pos = GeomTransform.Translate(coord.Longitude, coord.Latitude);
        
                    // 新しい ContourVertex をリストに追加
                    contourVertices.Add(new ContourVertex(new Vec3 { X = pos.X, Y = pos.Y, Z = 0 }));
                }
                // 生成された頂点を tess に追加
                tess.AddContour(contourVertices);
            }
        }
        tess.Tessellate(WindingRule.Positive);
        var points = new SKPoint[tess.ElementCount * 3];
        for (var j = 0; j < points.Length; j++)
        { 
            points[j] = new SKPoint(tess.Vertices[tess.Elements[j]].Position.X, 
                tess.Vertices[tess.Elements[j]].Position.Y);
        }
        Geometry = SKVertices.CreateCopy(SKVertexMode.Triangles, points, null);
    }
}

public class VectorLineFeature : VectorTileFeature
{
    public override SKPath? Geometry { get; }

    public VectorLineFeature(IEnumerable<MVectorTileFeature> features, TilePoint point)
    {
        var path = new SKPath();
        foreach (var feature in features)
        {
            foreach (var points in feature.Geometry)
            {
                path.AddPoly(points.Select(point1 => point1.ToPosition(point.X, point.Y, point.Z, GetFactor(point)))
                             .Select(coord => GeomTransform.Translate(coord.Longitude, coord.Latitude)).ToArray(), false);
            }
        }
        Geometry = path;
    }
}

public class VectorSymbolFeature : VectorTileFeature
{
    public SKTextBlob?[] Points { get; }
    public VectorSymbolFeature(IEnumerable<MVectorTileFeature> features, TilePoint point, SKFont font, string? fieldKey = "name")
    {
        if (fieldKey is null)
        {
            Points = [];
            return;
        }
        Points = (from feature in features
            let coord = feature.Geometry[0][0].ToPosition(point.X, point.Y, point.Z, GetFactor(point))
            let skPoint = GeomTransform.Translate(coord.Longitude, coord.Latitude)
            let text = feature.Attributes.FirstOrDefault(x => x.Key == fieldKey).Value?.ToString()
            let blob = SKTextBlob.Create(text, font, skPoint)
            select blob).ToArray();
    }

    public override void Dispose()
    {
        foreach (var textBlob in Points)
        {
            textBlob?.Dispose();
        }
        GC.SuppressFinalize(this);
    }
    
}