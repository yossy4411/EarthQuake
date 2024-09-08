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
            canvas.DrawLine(_geoTransform.Translate(i, 90), _geoTransform.Translate(i, -90), paint);
        }
        for (var i = -90; i <= 90; i += 15)
        {
            canvas.DrawLine(_geoTransform.Translate(-180, i), _geoTransform.Translate(180, i), paint);
        }
    }
}