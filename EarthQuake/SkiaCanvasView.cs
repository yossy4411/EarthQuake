using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using EarthQuake.Map;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake
{
    /// <summary>
    /// SKCanvas上に描画できるコントロールの抽象クラス
    /// </summary>
    public abstract class SkiaCanvasView : Control, IDisposable, ICustomDrawOperation
    {

        public void Dispose() => GC.SuppressFinalize(this);

        public bool Equals(ICustomDrawOperation? other) => false;

        public bool HitTest(Point p) => true;
        private protected static ISkiaSharpApiLease? GetSKCanvas(ImmediateDrawingContext context)
        {
            if (!context.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var feature)) { return null; }
            return feature.Lease();
        }
        public override void Render(DrawingContext context)
        {
            context.Custom(this);
        }
        public abstract void Render(ImmediateDrawingContext context);
    }
}
