using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia;
using HarfBuzzSharp;
using SkiaSharp;
using System;

namespace EarthQuake
{
    public class SelectableCanvas : MapCanvas
    {
        private Point start;
        private Point end;
        private bool selecting = false;
        public bool Locked { get => GetValue(LockedProperty); set => SetValue(LockedProperty, value); }
        public readonly static StyledProperty<bool> LockedProperty =
            AvaloniaProperty.Register<SelectableCanvas, bool>(
                nameof(Locked),
                false
            );
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (!Locked)
            {
                base.OnPointerPressed(e);
            }
            if (selecting = e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                end = start = e.GetPosition(this);
            }
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            
            base.OnPointerMoved(e);
            if (selecting)
            {
                end = e.GetPosition(this);
                if (!pressed) InvalidateVisual();
            }
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            selecting = false;
            base.OnPointerReleased(e);
            InvalidateVisual();
            Locked = Math.Abs(start.X - end.X) >= 2 && Math.Abs(start.Y - end.Y) >= 2;
        }
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            if (!Locked)
                base.OnPointerWheelChanged(e);
        }
        public override void Render(ImmediateDrawingContext context)
        {
            if (!context.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var feature)) { return; }
            using var lease = feature.Lease();
            var canvas = lease.SkCanvas;
            SKRect clipRect = new(0, 0, (float)Bounds.Width, (float)Bounds.Height);
            canvas.ClipRect(clipRect);
            SKPoint translate = Translate + Center;
            var region = new SKRect(-translate.X / Scale, -translate.Y / Scale, (float)(-translate.X + Bounds.Width) / Scale, (float)(-translate.Y + Bounds.Height) / Scale);
            float left = ((float)start.X - translate.X) / Scale;
            float top = ((float)start.Y - translate.Y) / Scale;
            float right = ((float)end.X - translate.X) / Scale;
            float bottom = ((float)end.Y - translate.Y) / Scale;
            SKRect selected = new(Math.Min(left, right), Math.Min(top, bottom), Math.Max(right, left), Math.Max(bottom, top));
            using (new SKAutoCanvasRestore(canvas))
            {
                canvas.Translate(Translate + Center);
                canvas.Scale(Scale);
                Controller?.RenderForeGround(canvas, Scale, region, selected);
                if (selecting)
                {
                    using SKPaint paint = new() { StrokeWidth = 2, PathEffect = SKPathEffect.CreateDash([7,3], 0), Style = SKPaintStyle.Stroke };
                    canvas.DrawRect(selected, paint);
                }
                
            }
           
            
        }
    }
}
