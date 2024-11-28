using EarthQuake.Core;
using EarthQuake.Map.Tiles.Vector;
using SkiaSharp;

namespace EarthQuake.Map.Layers;

/// <summary>
/// マップタイルのうち、地表を描画するレイヤー
/// </summary>
/// <param name="baseLayer"></param>
public class VectorMapLandLayer(VectorMapLayer baseLayer) : VectorMapLayer(null, baseLayer.Land)
{
    public override void Render(SKCanvas canvas, float scale, SKRect bounds)
    {
        
        if (Controller is null) return;
        var origin = GeomTransform.TranslateToNonTransform(bounds.Left, bounds.Top);
        
        VectorTilesController.GetXyzTile(origin, Math.Min(Header?.MaxZoom ?? 15, Math.Max(Header?.MinZoom ?? 5, (int)Math.Log2(scale) + 5)), out var point);
        VectorTilesController.GetXyzTileFromLatLon(Header?.MinLon ?? 0, Header?.MaxLat ?? 0, point.Z, out var leftTop);
        VectorTilesController.GetXyzTileFromLatLon(Header?.MaxLon ?? 0, Header?.MinLat ?? 0, point.Z, out var rightBottom);
        var zoom = (int)Math.Pow(2, point.Z);
        var h = (int)Math.Ceiling(bounds.Height / GeomTransform.Zoom / (GeomTransform.Height * 2f / zoom));
        var w = (int)Math.Ceiling(bounds.Width / GeomTransform.Zoom / (360f / zoom));
        h = Math.Min(h, zoom - point.Y);
        w = Math.Min(w, zoom - point.X);
        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.Typeface = Font;
        using var path = new SKPath();
        var widthFactor = point.Z switch
        {
            < 8 => 1,
            < 10 => 2,
            < 12 => 8,
            _ => 14
        }; // ズームするほど太くなっていっちゃうから調整
        for (var j = 0; j <= h; j++)
        {
            for (var i = 0; i <= w; i++)
            {
                var currentPoint = point.Add(i, j);
                if (currentPoint.X < leftTop.X || currentPoint.Y < leftTop.Y || currentPoint.X > rightBottom.X ||
                    currentPoint.Y > rightBottom.Y) continue;
                if (!Controller.TryGetTile(currentPoint, out var tile) || tile?.Vertices is null) continue;
                foreach (var feature in tile.Vertices)
                {
                    if (feature.Layer?.Id != Land) continue;  // Land以外は別のクラスで描画する
                    DrawLayer(canvas, scale, feature, paint, point, widthFactor, path);
                }
            }
        }
    }

    private protected override void Initialize()
    {
        baseLayer.Update();
        Controller = baseLayer.Controller;
        Header = baseLayer.Header;
    }
}