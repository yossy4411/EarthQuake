using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using EarthQuake.Map;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EarthQuake.Canvas.MapCanvas;

namespace EarthQuake.Canvas
{
    public class MapCanvasPair : Control
    {
        private MapCanvasTranslation _translation = new();
        public MapCanvasTranslation Translation
        {
            get => _translation; 
            set
            {
                _translation = value;
                if (BackgroundCanvas is not null) BackgroundCanvas.Translation = _translation;
                if (ForegroundCanvas is not null) ForegroundCanvas.Translation = _translation;
            }
        }

        public static readonly DirectProperty<MapCanvasPair, MapCanvasTranslation> TranslationProperty =
            AvaloniaProperty.RegisterDirect<MapCanvasPair, MapCanvasTranslation>(
                nameof(Translation),
                o => o.Translation,
                (o, value) => o.Translation = value

                );
        private MapViewController? _controller;
        public MapViewController? Controller
        {
            get => _controller; set
            {
                _controller = value;
                if (BackgroundCanvas is not null) BackgroundCanvas.Controller = _controller;
                if (ForegroundCanvas is not null) ForegroundCanvas.Controller = _controller;
            }
        }

        public static readonly DirectProperty<MapCanvasPair, MapViewController?> ControllerProperty =
            AvaloniaProperty.RegisterDirect<MapCanvasPair, MapViewController?>(
                nameof(Controller),
                o => o.Controller,
                (o, value) => o.Controller = value,
                null
                );

        public static readonly StyledProperty<MapCanvas?> BackgroundCanvasProperty =
            AvaloniaProperty.Register<MapCanvasPair, MapCanvas?>(nameof(BackgroundCanvas));

        public MapCanvas? BackgroundCanvas
        {
            get => GetValue(BackgroundCanvasProperty);
            set
            {
                if (value is not null)
                {
                    value.Controller = Controller;
                    value.Translation = Translation;
                }
                SetValue(BackgroundCanvasProperty, value);
            }
        }

        public static readonly StyledProperty<MapCanvas?> ForegroundCanvasProperty =
            AvaloniaProperty.Register<MapCanvasPair, MapCanvas?>(nameof(ForegroundCanvas));

        public MapCanvas? ForegroundCanvas
        {
            get => GetValue(ForegroundCanvasProperty);
            set
            {
                if (value is not null)
                {
                    value.Controller = Controller;
                    value.Translation = Translation;
                }
                SetValue(ForegroundCanvasProperty, value);
            }
        }
        private Point _scrollOffset;
        private protected SKPoint Offset => Translate + Center;


        public SKPoint Center => new((float)Bounds.Width / 2, (float)Bounds.Height / 2);
        private protected SKPoint Translate { get => Translation.Translate; set => Translation.Translate = value; }
        private protected float Scale { get => Translation.Scale; set => Translation.Scale = value; }
        private protected bool pressed;

        public override void Render(DrawingContext context)
        {
            BackgroundCanvas?.Render(context);
            ForegroundCanvas?.Render(context);
            base.Render(context);
        }
        protected override void OnInitialized()
        {
            base.OnInitialized();
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            // 子コントロールのサイズを測定する
            BackgroundCanvas?.Measure(availableSize);
            ForegroundCanvas?.Measure(availableSize);

            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var rect = new Rect(finalSize);
            BackgroundCanvas?.Arrange(rect);
            ForegroundCanvas?.Arrange(rect);

            return base.ArrangeOverride(finalSize);
        }



        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            _scrollOffset = e.GetPosition(this);
            pressed = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
            base.OnPointerPressed(e);
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (pressed)
            {
                var point = _scrollOffset - e.GetPosition(this);
                _scrollOffset = e.GetPosition(this);
                Translate = new(Translate.X - (float)point.X, Translate.Y - (float)point.Y);
                InvalidateVisual();
            }
            base.OnPointerMoved(e);
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            pressed = false;
            base.OnPointerReleased(e);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            var point = e.GetPosition(this);
            var zoomDelta = (float)Math.Pow(1.2f, e.Delta.Y);
            Scale *= zoomDelta;
            Translate = new(Translate.X + Translate.X * (zoomDelta - 1) - ((float)point.X - Center.X) * (zoomDelta - 1), Translate.Y + Translate.Y * (zoomDelta - 1) - ((float)point.Y - Center.Y) * (zoomDelta - 1));
            InvalidateVisual();
            base.OnPointerWheelChanged(e);
        }

    }
}
