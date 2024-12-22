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
    private TilePoint _point;
    protected internal readonly string Land = land;

    public override void Render(SKCanvas canvas, float scale, SKRect bounds)
    {
        if (Controller is null || styles is null) return;
        var origin = GeomTransform.TranslateToNonTransform(bounds.Left, bounds.Top);

        VectorTilesController.GetXyzTile(origin,
            Math.Min(Header?.MaxZoom ?? 15, Math.Max(Header?.MinZoom ?? 5, (int)Math.Log2(scale) + 5)), out var point);
        VectorTilesController.GetXyzTileFromLatLon(Header?.MinLon ?? 0, Header?.MaxLat ?? 0, point.Z, out var leftTop);
        VectorTilesController.GetXyzTileFromLatLon(Header?.MaxLon ?? 0, Header?.MinLat ?? 0, point.Z,
            out var rightBottom);
        var zoom = (int)Math.Pow(2, point.Z);
        var h = (int)Math.Ceiling(bounds.Height / GeomTransform.Zoom / (GeomTransform.Height * 2f / zoom));
        var w = (int)Math.Ceiling(bounds.Width / GeomTransform.Zoom / (360f / zoom));
        h = Math.Min(h, zoom - point.Y);
        w = Math.Min(w, zoom - point.X);
        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.Typeface = Font;
        var widthFactor = point.Z switch
        {
            < 8 => 1,
            < 10 => 2,
            < 12 => 8,
            _ => 14
        }; // ズームするほど太くなっていっちゃうから調整
        List<VectorTileFeature> features = [];
        List<SKRect> rects = [];
        for (var j = 0; j <= h; j++)
        {
            for (var i = 0; i <= w; i++)
            {
                var currentPoint = point.Add(i, j);
                if (currentPoint.X < leftTop.X || currentPoint.Y < leftTop.Y || currentPoint.X > rightBottom.X ||
                    currentPoint.Y > rightBottom.Y) continue;
                if (!Controller.TryGetTile(currentPoint, out var tile) || tile?.Vertices is null) continue;
                features.AddRange(tile.Vertices);
            }
        }


        foreach (var styleLayer in styles.Layers)
        {
            foreach (var feature in features)
            {
                if (feature.Layer?.Id != Land && ReferenceEquals(feature.Layer, styleLayer))
                {
                    DrawLayer(canvas, scale, feature, paint, point, widthFactor, rects);
                }
            }
        }
    }

    private protected static void DrawLayer(SKCanvas canvas, float scale, VectorTileFeature feature, SKPaint paint,
        TilePoint point, int widthFactor, List<SKRect> rects)
    {
        switch (feature)
        {
            case VectorLineFeature line:
            {
                if (feature.Layer is not VectorLineStyleLayer layer) return;
                paint.Color = layer.LineColor is null
                    ? SKColors.White
                    : layer.LineColor.GetValue(feature.Tags)!.ToColor().ToSKColor();
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

                canvas.DrawPath(line.Path, paint);
                break;
            }
            case VectorFillFeature fill:
            {
                if (feature.Layer is not VectorFillStyleLayer layer) return;
                if (fill.Vertices is null) return;
                paint.Color = layer.FillColor is null
                    ? SKColors.White
                    : layer.FillColor.GetValue(feature.Tags)!.ToColor().ToSKColor();
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawVertices(fill.Vertices, SKBlendMode.Src, paint);
                break;
            }
            case VectorSymbolFeature symbol:
            {
                if (feature.Layer is not VectorSymbolStyleLayer layer || symbol.Text is null) return;

                paint.TextSize = (layer.TextSize is null ? 15 : layer.TextSize.GetValue(feature.Tags)!.ToFloat()) /
                                 scale;
                paint.Color = layer.TextColor is null
                    ? SKColors.White
                    : layer.TextColor!.GetValue(feature.Tags)!.ToColor().ToSKColor();
                paint.Style = SKPaintStyle.Fill;
                var rect = new SKRect(symbol.Point.X, symbol.Point.Y - paint.TextSize,
                    symbol.Point.X + paint.MeasureText(symbol.Text), symbol.Point.Y);
                if (rects.Any(x => x.IntersectsWith(rect))) return; // 重なっていたら描画しない
                rects.Add(rect);
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
    }

    public override bool ShouldReload(float zoom, SKRect bounds)
    {
        var origin = GeomTransform.TranslateToNonTransform(bounds.MidX, bounds.MidY);
        VectorTilesController.GetXyzTile(origin, (int)Math.Log2(zoom) + 5, out var point);
        // 表示範囲のタイルが変わったか
        if (_point == point) return false;
        _point = point;
        return true;
    }
}