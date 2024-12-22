using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using System;

namespace EarthQuake;

/// <summary>
/// SKCanvas上に描画できるコントロールの抽象クラス
/// </summary>
public abstract class SkiaCanvasView : Control, ICustomDrawOperation
{
    public void Dispose() => GC.SuppressFinalize(this);

    public bool Equals(ICustomDrawOperation? other) => false;

    public bool HitTest(Point p) => true;

    private protected static ISkiaSharpApiLease? GetSKCanvas(ImmediateDrawingContext context)
    {
        return !context.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var feature) ? null : feature.Lease();
    }

    public override void Render(DrawingContext context)
    {
        context.Custom(this);
    }

    public abstract void Render(ImmediateDrawingContext context);
}