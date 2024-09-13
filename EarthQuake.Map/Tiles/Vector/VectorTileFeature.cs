using EarthQuake.Core;
using LibTessDotNet;
using Mapbox.Vector.Tile;
using SkiaSharp;
using MVectorTileFeature = Mapbox.Vector.Tile.VectorTileFeature;

namespace EarthQuake.Map.Tiles.Vector;

public abstract class VectorTileFeature : IDisposable
{
    public virtual SKObject? Geometry { get; } = null;
    public VectorTileMapLayer Layer { get; init; } = null!;

    public virtual void Dispose()
    {
        if (Geometry is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
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
            foreach (var coords in feature.Geometry.Select(coordinates => from v in coordinates 
                         let coord = v.ToPosition(point.X, point.Y, point.Z, point.Z < 8 ? 16384u : 4096u)
                         let pos = GeomTransform.Translate(coord.Longitude, coord.Latitude)
                         select new ContourVertex(new Vec3 { X = pos.X, Y = pos.Y, Z = 0 })))
            {
                tess.AddContour(coords.ToList());
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
                path.AddPoly(points.Select(point1 => point1.ToPosition(point.X, point.Y, point.Z, point.Z < 8 ? 16384u : 4096u))
                             .Select(coord => GeomTransform.Translate(coord.Longitude, coord.Latitude)).ToArray(), false);
            }
        }
        Geometry = path;
    }
}

public class VectorSymbolFeature : VectorTileFeature
{
    public (SKTextBlob?,SKPoint)[] Points { get; }
    public VectorSymbolFeature(IEnumerable<MVectorTileFeature> features, TilePoint point, SKFont font, string? fieldKey = "name")
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
            select (SKTextBlob.Create(text, font), skPoint)).ToArray();
    }
    
    public override void Dispose()
    {
        foreach (var (textBlob, _) in Points)
        {
            textBlob?.Dispose();
        }
        GC.SuppressFinalize(this);
    }
    
}