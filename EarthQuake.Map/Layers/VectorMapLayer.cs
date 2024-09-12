using EarthQuake.Core;
using EarthQuake.Map.Tiles;
using EarthQuake.Map.Tiles.Vector;
using SkiaSharp;

namespace EarthQuake.Map.Layers;

/// <summary>
/// ベクトルタイルを描画するレイヤー
/// </summary>
/// <param name="styles"></param>
public class VectorMapLayer(VectorMapStyles styles, string url) : MapLayer
{
    private VectorTilesController? _controller;
    private VectorMapStyles? _styles = styles;

    internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
    {
        if (_controller is null) return;
        var origin = GeomTransform.TranslateToNonTransform(bounds.Left, bounds.Top);
        VectorTilesController.GetXyzTile(origin, (int)Math.Log2(scale) + 5, out var point);
        var zoom = (int)Math.Pow(2, point.Z);
        var h = (int)Math.Ceiling(bounds.Height / GeomTransform.Zoom / (GeomTransform.Height * 2f / zoom));
        var w = (int)Math.Ceiling(bounds.Width / GeomTransform.Zoom / (360f / zoom));
        h = Math.Min(h, zoom - point.Y);
        w = Math.Min(w, zoom - point.X);
        var z = MathF.Log(scale, 2) + 5;
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
                            var layer = (VectorFillLayer?)feature.Layer;
                            if (layer is null) continue;
                            paint.Color = layer.FillColor;
                            paint.Style = SKPaintStyle.Fill;
                            if (feature.Geometry is not null)
                            {
                                canvas.DrawVertices(feature.Geometry, SKBlendMode.SrcOver, paint);
                            }

                            break;
                        }
                        case VectorLineFeature lineFeature:
                        {
                            var layer = (VectorLineLayer?)lineFeature.Layer;
                            if (layer is null) continue;
                            paint.Color = layer.LineColor;
                            paint.StrokeWidth = layer.GetWidth(z) / scale;
                            paint.Style = SKPaintStyle.Stroke;
                            paint.PathEffect = layer.PathEffect;
                            if (lineFeature.Geometry is not null)
                            {
                                canvas.DrawPath(lineFeature.Geometry, paint);
                            }

                            break;
                        }
                        case VectorSymbolFeature symbolFeature:
                        {
                            var layer = (VectorSymbolLayer?)symbolFeature.Layer;
                            if (layer is null) continue;
                            paint.Color = layer.TextColor;
                            paint.Style = SKPaintStyle.Fill;
                            foreach (var (text, skPoint) in symbolFeature.Points)
                            {
                                using (new SKAutoCanvasRestore(canvas))
                                {
                                    canvas.Translate(skPoint);
                                    canvas.Scale(layer.FontSize　/ scale / 16f);
                                    canvas.DrawText(text, 0, 0, paint);
                                }
                            }
                            break;
                        }
                    }
                }
                
            }
        }
    }

    private protected override void Initialize()
    {
        _controller = new VectorTilesController(url, _styles!);
        _styles = null; // 参照を解放
    }
}