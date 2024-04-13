using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake
{
    public class RotationableMapCanvas : MapCanvas
    {
        public float Rotation { get; set; } = 0;
        public override void Render(ImmediateDrawingContext context)
        {
            if (!context.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var feature)) { return; }
            using var lease = feature.Lease();
            var canvas = lease.SkCanvas;
            SKRect clipRect = new(0, 0, (float)Bounds.Width, (float)Bounds.Height);
            canvas.ClipRect(clipRect);
            canvas.Clear(Background);
            SKPoint translate = Translate + Center;
            var region = new SKRect(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);
            using (new SKAutoCanvasRestore(canvas))
            {
                using var view = new SK3dView();
                view.RotateXDegrees(Rotation);
                canvas.Translate(Translate + Center);
                canvas.Scale(Scale);
                view.ApplyToCanvas(canvas);
                Controller?.RenderBase(canvas, Scale, region);
            }
        }
    }
}
