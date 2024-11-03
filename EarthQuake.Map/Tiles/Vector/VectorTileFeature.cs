using EarthQuake.Core;
using LibTessDotNet;
using SkiaSharp;
using VectorTiles.Styles;
using VectorTiles.Styles.Values;
using VectorTiles.Values;
using MVectorTileFeature = VectorTiles.Mvt.MapboxTile.Layer.Feature;

namespace EarthQuake.Map.Tiles.Vector;

public abstract class VectorTileFeature : IDisposable
{
    public virtual SKObject? Geometry => null;
    internal VectorMapStyleLayer Layer { get; init; } = null!;
    
    internal Dictionary<string, IConstValue?> Tags { get; init; } = null!;

    public virtual void Dispose()
    {
        if (Geometry is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    public T? GetPropertyValue<T>(StyleProperty<T>? property) => property is null ? default : property.GetValue(Tags);
}

public class VectorFillFeature : VectorTileFeature
{
    public override SKVertices? Geometry { get; }

    public VectorFillFeature(MVectorTileFeature feature, TilePoint point)
    {
        var tess = new Tess();
        if (feature.Geometries.Count <= 0) return;
        foreach (var coordinates in feature.Geometries)
        {
            var contourVertices = new List<ContourVertex>(coordinates.Points.Count); // 結果を格納するリスト
            foreach (var v in coordinates.Points)
            {
                // pos を計算
                var pos = GeomTransform.Translate(v.Lon, v.Lat);
    
                // 新しい ContourVertex をリストに追加
                contourVertices.Add(new ContourVertex(new Vec3 { X = pos.X, Y = pos.Y, Z = 0 }));
            }
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
        Geometry = SKVertices.CreateCopy(SKVertexMode.Triangles, points, null);
    }
}

public class VectorLineFeature : VectorTileFeature
{
    public override SKPath? Geometry { get; }

    public VectorLineFeature(MVectorTileFeature feature, TilePoint point)
    {
        var path = new SKPath();

        foreach (var points in feature.Geometries)
        {
            path.AddPoly(points.Points.Select(a => GeomTransform.Translate(a.Lon, a.Lat)).ToArray(), false);
        }
        
        Geometry = path;
    }
}

public class VectorSymbolFeature : VectorTileFeature
{
    public SKTextBlob? Text { get; }
    public VectorSymbolFeature(MVectorTileFeature feature, TilePoint point, SKFont font)
    {
        var field = (Layer as VectorSymbolStyleLayer)?.TextField;
        if (field is null)
        {
            Text = null;
            return;
        }

        var coord = feature.Geometries[0].Points[0];
        var skPoint = GeomTransform.Translate(coord.Lon, coord.Lat);
        var text = feature.Tags.GetValueOrDefault(field)?.ToString();
        var blob = SKTextBlob.Create(text, font, skPoint);
        Text = blob;
    }

    public override void Dispose()
    {
        Text?.Dispose();
        GC.SuppressFinalize(this);
    }
    
}