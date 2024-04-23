using Avalonia.Media;
using EarthQuake.Core;
using SkiaSharp;
using System.Drawing;

namespace EarthQuake.Map.Layers
{
    public class GridLayer : MapLayer
    {
        private GeomTransform _geoTransform = new();

        private protected override void Initialize(GeomTransform geo)
        {
            _geoTransform = geo;
        }

        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            using var paint = new SKPaint()
            {
                Color = SKColors.Gray
            };
            for (int i = -180; i <= 180; i += 15)
            {
                canvas.DrawLine(_geoTransform.Translate(i, 90), _geoTransform.Translate(i, -90), paint);
            }
            for (int i = -90; i <= 90; i += 15)
            {
                canvas.DrawLine(_geoTransform.Translate(-180, i), _geoTransform.Translate(180, i), paint);
            }
        }
    }
}
