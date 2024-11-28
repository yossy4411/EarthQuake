using EarthQuake.Core;
using EarthQuake.Map.Tiles;
using EarthQuake.Map.Tiles.Vector;
using PMTiles;
using SkiaSharp;
using VectorTiles.Styles;

namespace EarthQuake.Map.Layers;

/// <summary>
/// ベクトルタイルを描画するレイヤー
/// </summary>
/// <param name="styles"></param>
public class VectorMapLayer(VectorMapStyle? styles, string land) : CacheableLayer
{
    protected internal VectorTilesController? Controller;
    protected internal Header? Header;
    private VectorMapStyle? styles = styles;
    private TilePoint _point;
    protected internal string Land = land;

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
                    if (feature.Layer?.Id == Land) continue;  // Landレイヤーは別のクラスで描画する
                    DrawLayer(canvas, scale, feature, paint, point, widthFactor, path);
                }
            }
        }
    }

    private protected static void DrawLayer(SKCanvas canvas, float scale, VectorTileFeature feature, SKPaint paint,
        TilePoint point, int widthFactor, SKPath path)
    {
        switch (feature)
        {
            case VectorLineFeature line:
            {
                if (feature.Layer is not VectorLineStyleLayer layer) return;
                paint.Color = layer.LineColor is null ? SKColors.White : layer.LineColor.GetValue(feature.Tags)!.ToColor().ToSKColor();
                paint.StrokeWidth =
                    (layer.LineWidth is null ? 1 : layer.LineWidth.GetValue(feature.Tags)!.ToFloat()) /
                    (point.Z > 12 ? 5000f : scale) / widthFactor;
                paint.StrokeCap = SKStrokeCap.Round;
                paint.StrokeJoin = SKStrokeJoin.Round;
                paint.Style = SKPaintStyle.Stroke;

                if (layer.DashArray is not null)
                {
                    using var pathEffect =
                        SKPathEffect.CreateDash(layer.DashArray.Select(x => x / scale).ToArray(), 1);
                    paint.PathEffect = pathEffect;
                }
                else
                {
                    paint.PathEffect = null;
                }
                            
                            
                foreach (var skPoints in line.Geometry)
                {
                    path.AddPoly(skPoints, false);
                }

                canvas.DrawPath(path, paint);
                path.Reset();
                break;
            }
            case VectorFillFeature fill:
            {
                if (feature.Layer is not VectorFillStyleLayer layer) return;
                paint.Color = layer.FillColor is null ? SKColors.White : layer.FillColor.GetValue(feature.Tags)!.ToColor().ToSKColor();
                paint.Style = SKPaintStyle.Fill;
                using var vertices =
                    SKVertices.CreateCopy(SKVertexMode.Triangles, fill.Geometry, null);
                canvas.DrawVertices(vertices, SKBlendMode.Src, paint);
                break;
            }
            case VectorSymbolFeature symbol:
            {
                if (feature.Layer is not VectorSymbolStyleLayer layer || symbol.Text is null) return;

                paint.TextSize = (layer.TextSize is null ? 15 : layer.TextSize.GetValue(feature.Tags)!.ToFloat()) / scale;
                paint.Color = layer.TextColor is null ? SKColors.White : layer.TextColor!.GetValue(feature.Tags)!.ToColor().ToSKColor();
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawText(symbol.Text, symbol.Point, paint);
                break;
            }
        }
    }

    private protected override void Initialize()
    {
        Controller = new VectorTilesController(styles!)
        {
            OnUpdate = HandleUpdated
        };
        if (Controller.PMTiles != null) Header = Controller.PMTiles.GetHeader();

        styles = null; // 参照を解放
    }

    public override bool IsReloadRequired(float zoom, SKRect bounds)
    {
        var origin = GeomTransform.TranslateToNonTransform(bounds.Left, bounds.Top);
        VectorTilesController.GetXyzTile(origin, (int)Math.Log2(zoom) + 5, out var point);
        // 表示範囲のタイルが変わったか
        if (_point == point) return false;
        _point = point;
        return true;
    }
}