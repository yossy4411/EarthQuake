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
public class VectorMapLayer(VectorMapStyle styles) : CacheableLayer
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
                if (!_controller.TryGetTile(point.Add(i, j), out var tile) || tile?.Vertices is null) continue;
                foreach (var feature in tile.Vertices)
                {
                    switch (feature)
                    {
                        case VectorLineFeature line:
                        {
                            if (feature.Layer is not VectorLineStyleLayer layer) continue;
                            paint.Color = layer.LineColor is null ? SKColors.White : layer.LineColor.GetValue(feature.Tags)!.ToColor().ToSKColor();
                            paint.StrokeWidth =
                                (layer.LineWidth is null ? 1 : layer.LineWidth.GetValue(feature.Tags)!.ToFloat()) / scale / widthFactor;
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
                            if (feature.Layer is not VectorFillStyleLayer layer) continue;
                            paint.Color = layer.FillColor is null ? SKColors.White : layer.FillColor.GetValue(feature.Tags)!.ToColor().ToSKColor();
                            paint.Style = SKPaintStyle.Fill;
                            using var vertices =
                                SKVertices.CreateCopy(SKVertexMode.Triangles, fill.Geometry, null);
                            canvas.DrawVertices(vertices, SKBlendMode.Src, paint);
                            break;
                        }
                        case VectorSymbolFeature symbol:
                        {
                            if (feature.Layer is not VectorSymbolStyleLayer layer || symbol.Text is null) continue;

                            paint.TextSize = (layer.TextSize is null ? 15 : layer.TextSize.GetValue(feature.Tags)!.ToFloat()) / scale;
                            paint.Color = layer.TextColor is null ? SKColors.White : layer.TextColor!.GetValue(feature.Tags)!.ToColor().ToSKColor();
                            paint.Style = SKPaintStyle.Fill;
                            canvas.DrawText(symbol.Text, symbol.Point, paint);
                            break;
                        }
                    }
                }
            }
        }
    }

    private protected override void Initialize()
    {
        _controller = new VectorTilesController(_styles!)
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