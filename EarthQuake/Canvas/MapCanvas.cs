﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using EarthQuake.Map;
using System;
using System.Diagnostics;
using Avalonia.Threading;
using SkiaSharp;
using Newtonsoft.Json.Bson;

namespace EarthQuake.Canvas
{
    public class MapCanvas : SkiaCanvasView
    {
        public class MapCanvasTranslation
        {
            public SKPoint Translate { get; set; } = new();
            public float Scale { get; set; } = 1f;
        }

        public MapViewController? Controller
        {
            get => controller;
            set
            {
                controller = value;
                if (controller is not null)
                {
                    // 描画スレッドでInvalidateVisualを呼び出す。(誤ったスレッドで呼び出すと例外が発生するため)
                    controller.OnUpdated += () =>
                    {
                        Dispatcher.UIThread.Post(InvalidateVisual);
                        Debug.WriteLine($"InvalidateVisual for Canvas [{Name}]");
                    };
                }
            }
        }

        public static readonly DirectProperty<MapCanvas, MapViewController?> ControllerProperty =
            AvaloniaProperty.RegisterDirect<MapCanvas, MapViewController?>(
                nameof(Controller),
                o => o.Controller,
                (o, value) => o.Controller = value,
                null
                );
        public virtual SKColor Background => SKColors.LightBlue;
        private Point _scrollOffset;
        private protected SKPoint Offset => Translate + Center;


        public SKPoint Center => new((float)Bounds.Width / 2, (float)Bounds.Height / 2);
        private protected SKPoint Translate { get => Translation.Translate; set => Translation.Translate = value; }
        private protected float Scale { get => Translation.Scale; set => Translation.Scale = value; }

        public MapCanvasTranslation Translation { get; set; } = new();
        public static readonly DirectProperty<MapCanvas, MapCanvasTranslation> TranslationProperty =
            AvaloniaProperty.RegisterDirect<MapCanvas, MapCanvasTranslation>(
                nameof(Translation),
                o => o.Translation,
                (o, value) => o.Translation = value

                );
        private protected bool pressed;
        private MapViewController? controller;

        public override void Render(ImmediateDrawingContext context)
        {
            using var lease = GetSKCanvas(context);
            if (lease is null) return;
            var canvas = lease.SkCanvas;

            SKRect clipRect = new(0, 0, (float)Bounds.Width, (float)Bounds.Height);
            canvas.ClipRect(clipRect);
            canvas.Clear(Background);
            var translate = Translate + Center;
            var region = new SKRect(-translate.X / Scale, -translate.Y / Scale, (float)(-translate.X + Bounds.Width) / Scale, (float)(-translate.Y + Bounds.Height) / Scale);
            using (new SKAutoCanvasRestore(canvas))
            {
                canvas.Translate(Translate + Center);
                canvas.Scale(Scale);

                Controller?.Render(canvas, Scale, region);
            }

            
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
