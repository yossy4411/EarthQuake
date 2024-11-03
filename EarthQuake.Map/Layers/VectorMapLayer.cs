using EarthQuake.Core;
using EarthQuake.Map.Tiles;
using EarthQuake.Map.Tiles.Vector;
using SkiaSharp;
using VectorTiles.Styles;

namespace EarthQuake.Map.Layers;

/// <summary>
/// ベクトルタイルを描画するレイヤー
/// </summary>
/// <param name="styles"></param>
public class VectorMapLayer(VectorMapStyle styles, string url) : CacheableLayer
{
    private VectorTilesController? _controller;
    private VectorMapStyle? _styles = styles;
    private TilePoint _point;

    public override void Render(SKCanvas canvas, float scale, SKRect bounds)
    {
        if (_controller is null) return;
        var origin = GeomTransform.TranslateToNonTransform(bounds.Left, bounds.Top);
        VectorTilesController.GetXyzTile(origin, (int)Math.Log2(scale) + 5, out var point);
        var zoom = (int)Math.Pow(2, point.Z);
        var h = (int)Math.Ceiling(bounds.Height / GeomTransform.Zoom / (GeomTransform.Height * 2f / zoom));
        var w = (int)Math.Ceiling(bounds.Width / GeomTransform.Zoom / (360f / zoom));
        h = Math.Min(h, zoom - point.Y);
        w = Math.Min(w, zoom - point.X);
        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.Typeface = Font;
        for (var j = 0; j <= h; j++)
        {
            for (var i = 0; i <= w; i++)
            {
                if (!_controller.TryGetTile(point.Add(i, j), out var tile) || tile?.Vertices is null) continue;
                foreach (var vectorTileFeature in tile.Vertices)
                {
                    switch (vectorTileFeature)
                    {
                        case VectorFillFeature feature:
                        {
                            if (feature.Layer is not VectorFillStyleLayer layer) continue;
                            paint.Color = feature.GetPropertyValue(layer.FillColor).ToSKColor();
                            paint.Style = SKPaintStyle.Fill;
                            if (feature.Geometry is not null)
                            {
                                canvas.DrawVertices(feature.Geometry, SKBlendMode.SrcOver, paint);
                            }

                            break;
                        }
                        case VectorLineFeature lineFeature:
                        {
                            if (lineFeature.Layer is not VectorLineStyleLayer layer) continue;
                            paint.Color = lineFeature.GetPropertyValue(layer.LineColor).ToSKColor();
                            paint.StrokeWidth = lineFeature.GetPropertyValue(layer.LineWidth) / scale;
                            paint.Style = SKPaintStyle.Stroke;
                            if (layer.DashArray is not null)
                            {
                                using var pathEffect = SKPathEffect.CreateDash(layer.DashArray.Select(x => x / scale).ToArray(), 1);
                                paint.PathEffect = pathEffect;
                            }
                            else
                            {
                                paint.PathEffect = null;
                            }

                            if (lineFeature.Geometry is not null)
                            {
                                canvas.DrawPath(lineFeature.Geometry, paint);
                            }

                            break;
                        }
                        case VectorSymbolFeature symbolFeature:
                        {
                            if (symbolFeature.Layer is not VectorSymbolStyleLayer layer || symbolFeature.Text is null) continue;
                            paint.Color = symbolFeature.GetPropertyValue(layer.TextColor).ToSKColor();
                            paint.Style = SKPaintStyle.Fill;
                            canvas.DrawText(symbolFeature.Text, 0, 0, paint);

                            break;
                        }
                    }
                }
            }
        }
    }

    private protected override void Initialize()
    {
        _controller = new VectorTilesController(url, _styles!)
        {
            OnUpdate = HandleUpdated
        };
        _styles = null; // 参照を解放
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