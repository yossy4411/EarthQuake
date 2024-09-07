using EarthQuake.Core;
using SkiaSharp;

namespace EarthQuake.Map.Layers;

public class GridLayer : MapLayer
{
    private GeomTransform _geoTransform = new();

    private protected override void Initialize(GeomTransform geo)
    {
        _geoTransform = geo;
    }

    internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
    {
        using var paint = new SKPaint();
        paint.Color = SKColors.Gray;
        for (var i = -180; i <= 180; i += 15)
        {
            canvas.DrawLine(GeomTransform.Translate(i, 90), GeomTransform.Translate(i, -90), paint);
        }
        for (var i = -90; i <= 90; i += 15)
        {
            canvas.DrawLine(GeomTransform.Translate(-180, i), GeomTransform.Translate(180, i), paint);
        }
    }
}