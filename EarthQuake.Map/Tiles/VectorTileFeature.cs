using EarthQuake.Core;
using LibTessDotNet;
using Mapbox.Vector.Tile;
using SkiaSharp;
using MVectorTileFeature = Mapbox.Vector.Tile.VectorTileFeature;

namespace EarthQuake.Map.Tiles;

public abstract class VectorTileFeature
{
    public virtual SKObject? Geometry { get; }
    public VectorTileMapLayer Layer { get; init; } = null!;
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
                tess.AddContour(coordinates.Select(x =>
                {
                    var coord = x.ToPosition(point.X, point.Y, point.Z, point.Z < 8 ? 16384u : 4096u);
                    var pos = GeomTransform.Translate(coord.Longitude, coord.Latitude);
                    return new ContourVertex(new Vec3 { X = pos.X, Y = pos.Y, Z = 0 });
                }).ToArray());
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
                var first = true;
                foreach (var pos in points.Select(point1 => point1.ToPosition(point.X, point.Y, point.Z, point.Z < 8 ? 16384u : 4096u))
                             .Select(coord => GeomTransform.Translate(coord.Longitude, coord.Latitude)))
                {
                    if (first)
                    {
                        path.MoveTo(pos);
                        first = false;
                    }
                    else
                    {
                        path.LineTo(pos);
                    }
                }
            }
        }
        Geometry = path;
    }
}

public class VectorSymbolFeature : VectorTileFeature
{
    public (SKPoint, string?)[] Points { get; }
    public VectorSymbolFeature(IEnumerable<MVectorTileFeature> features, TilePoint point, string? fieldKey = "name")
    {
        if (fieldKey is null)
        {
            Points = [];
            return;
        }
        Points = (from feature in features
            let coord = feature.Geometry[0][0].ToPosition(point.X, point.Y, point.Z, point.Z < 8 ? 16384u : 4096u)
            let skPoint = GeomTransform.Translate(coord.Longitude, coord.Latitude)
            let text = feature.Attributes.FirstOrDefault(x => x.Key == fieldKey).Value?.ToString()
            select (skPoint, text)).ToArray();
    }
    
}