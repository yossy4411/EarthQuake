using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace EarthQuake.Canvas
{
    public class OverlayCanvas : MapCanvas
    {
        public override void Render(ImmediateDrawingContext context)
        {
            if (!context.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var feature)) { return; }
            using var lease = feature.Lease();
            var canvas = lease.SkCanvas;
            SKRect clipRect = new(0, 0, (float)Bounds.Width, (float)Bounds.Height);
            canvas.ClipRect(clipRect);
            SKPoint translate = Translate + Center;
            var region = new SKRect(-translate.X / Scale, -translate.Y / Scale, (float)(-translate.X + Bounds.Width) / Scale, (float)(-translate.Y + Bounds.Height) / Scale);
            using (new SKAutoCanvasRestore(canvas))
            {
                canvas.Translate(Translate + Center);

                Controller?.RenderForeGround(canvas, Scale, region);
            }
        }
    }
}
