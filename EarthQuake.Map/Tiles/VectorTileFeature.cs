using EarthQuake.Core;
using LibTessDotNet;
using Mapbox.Vector.Tile;
using SkiaSharp;
using MVectorTileFeature = Mapbox.Vector.Tile.VectorTileFeature;

namespace EarthQuake.Map.Tiles;

public abstract class VectorTileFeature
{
    public abstract IReadOnlyDictionary<string, string>? Properties { get; init; }
    public abstract SKObject? Geometry { get; }
}

public class VectorFillFeature : VectorTileFeature
{
    public override SKVertices? Geometry { get; }
    public override IReadOnlyDictionary<string, string>? Properties { get; init; }

    public VectorFillFeature(IEnumerable<MVectorTileFeature> features, TilePoint point)
    {
        var tess = new Tess();
        foreach (var feature in features)
        {
            if (feature.Geometry.Count <= 0) return;
            tess.AddContour(feature.Geometry[0].Select(x => { var coord = x.ToPosition(point.X, point.Y, point.Z, 256);
            var pos = GeomTransform.Translate(coord.Longitude, coord.Latitude); return new ContourVertex(new Vec3 { X = pos.X, Y = pos.Y, Z = 0 }); }).ToArray());
        }
        tess.Tessellate(WindingRule.Positive);
        var points = tess.Vertices.Select(x => new SKPoint(x.Position.X, x.Position.Y)).ToArray();
        Geometry = SKVertices.CreateCopy(SKVertexMode.Triangles, points, null, null);
    }
}

public class VectorLineFeature : VectorTileFeature
{
    public override SKPath? Geometry { get; }
    public override IReadOnlyDictionary<string, string>? Properties { get; init; }

    public VectorLineFeature(IEnumerable<MVectorTileFeature> features, TilePoint point)
    {
        var path = new SKPath();
        foreach (var feature in features)
        {
            foreach (var points in feature.Geometry)
            {
                var first = true;
                foreach (var pos in points.Select(point1 => point1.ToPosition(point.X, point.Y, point.Z, 256))
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