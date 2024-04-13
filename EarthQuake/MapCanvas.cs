using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using EarthQuake.Map;
using System;
using SkiaSharp;

namespace EarthQuake
{
    public class MapCanvas : Control, IDisposable, ICustomDrawOperation
    {
        public class MapCanvasTranslation
        {
            public SKPoint Translate { get; set; } = new();
            public float Scale { get; set; } = 1f;
        }
        public MapViewController? Controller { get; set; }
        public static readonly DirectProperty<MapCanvas, MapViewController?> ControllerProperty =
            AvaloniaProperty.RegisterDirect<MapCanvas, MapViewController?>(
                nameof(Controller),
                o => o.Controller,
                (o, value) => o.Controller = value,
                null
                );

        public virtual SKColor Background => SKColors.LightBlue;
        private Point offset;
        public SKPoint Center => new((float)Bounds.Width / 2, (float)Bounds.Height / 2);
        private protected SKPoint Translate { get => Translation.Translate; set => Translation.Translate = value; }
        private protected float Scale { get=> Translation.Scale; set => Translation.Scale = value; }
        public MapCanvasTranslation Translation { get; set; } = new();
        public static readonly DirectProperty<MapCanvas, MapCanvasTranslation> TranslationProperty =
            AvaloniaProperty.RegisterDirect<MapCanvas, MapCanvasTranslation>(
                nameof(Translation),
                o => o.Translation,
                (o, value) => o.Translation = value
                
                );
        private bool Pressed;
        public void Dispose() => GC.SuppressFinalize(this);

        public bool Equals(ICustomDrawOperation? other) => false;

        public bool HitTest(Point p) => true;

        public override void Render(DrawingContext context)
        {
            context.Custom(this);
        }

        public virtual void Render(ImmediateDrawingContext context)
        {
            if (!context.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var feature)) { return; }
            using var lease = feature.Lease();
            var canvas = lease.SkCanvas;
            SKRect clipRect = new(0, 0, (float)Bounds.Width, (float)Bounds.Height);
            canvas.ClipRect(clipRect);
            canvas.Clear(Background);
            SKPoint translate = Translate + Center;
            var region = new SKRect(-translate.X / Scale, -translate.Y / Scale, (float)(-translate.X + Bounds.Width) / Scale, (float)(-translate.Y + Bounds.Height) / Scale);
            using (new SKAutoCanvasRestore(canvas))
            {
                canvas.Translate(Translate + Center);
                canvas.Scale(Scale);

                Controller?.RenderBase(canvas, Scale, region);
            }
        }


        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            offset = e.GetPosition(this);
            Pressed = true;
            base.OnPointerPressed(e);
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (Pressed)
            {
                Point point = offset - e.GetPosition(this);
                offset = e.GetPosition(this);
                Translate = new(Translate.X-(float)point.X, Translate.Y-(float)point.Y);
                InvalidateVisual();
            }
            base.OnPointerMoved(e);
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            Pressed = false;
            base.OnPointerReleased(e);
        }
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            Point point = e.GetPosition(this);
            var zoomDelta = (float)Math.Pow(1.2f, e.Delta.Y);
            Scale *= zoomDelta;
            Translate = new(Translate.X + Translate.X * (zoomDelta - 1) - ((float)point.X - Center.X) * (zoomDelta - 1), Translate.Y + Translate.Y * (zoomDelta - 1) - ((float)point.Y - Center.Y) * (zoomDelta - 1));
            InvalidateVisual();
            base.OnPointerWheelChanged(e);
        }
    }
}
