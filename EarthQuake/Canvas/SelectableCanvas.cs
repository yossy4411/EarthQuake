﻿using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;
using System;

namespace EarthQuake.Canvas;

public class SelectionEventArgs(SKRect selected) : EventArgs
{
    public SKRect Selected { get; } = selected;
}

/// <summary>
/// 選択可能なキャンバス
/// </summary>
public class SelectableCanvas : MapCanvas
{
    private bool selecting;
    private SKRect Selected { get; set; }
    private float _startX;
    private float _startY;
    private float _endX;
    private float _endY;
    public event EventHandler<SelectionEventArgs>? OnSelected;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        selecting = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
        if (!selecting) return;
        var point = e.GetPosition(this);
        TranslateBack((float)point.X, (float)point.Y, out var x, out var y);
        _endX = _startX = x;
        _endY = _startY = y;
        Selected = SKRect.Empty;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!selecting) return;
        var point = e.GetPosition(this);
        TranslateBack((float)point.X, (float)point.Y, out var x, out var y);
        _endX = x;
        _endY = y;
        var left = _startX;
        var top = _startY;
        var right = _endX;
        var bottom = _endY;
        Selected = new(Math.Min(left, right), Math.Min(top, bottom), Math.Max(right, left), Math.Max(bottom, top));
        if (!Pressed) InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        InvalidateVisual();
        if (Selected is { Width: > 2, Height: > 2 } && selecting)
            OnSelected?.Invoke(this, new SelectionEventArgs(Selected));
        selecting = false;
    }

    private void TranslateBack(float x, float y, out float left, out float top)
    {
        left = (x - Offset.X) / Scale;
        top = (y - Offset.Y) / Scale;
    }

    public override void Render(ImmediateDrawingContext context)
    {
        if (!context.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var feature))
        {
            return;
        }

        using var lease = feature.Lease();
        var canvas = lease.SkCanvas;
        SKRect clipRect = new(0, 0, (float)Bounds.Width, (float)Bounds.Height);
        canvas.ClipRect(clipRect);
        canvas.Clear(Background);
        var region = new SKRect(-Offset.X / Scale, -Offset.Y / Scale, (float)(-Offset.X + Bounds.Width) / Scale,
            (float)(-Offset.Y + Bounds.Height) / Scale);
        using (new SKAutoCanvasRestore(canvas))
        {
            canvas.Translate(Offset);
            canvas.Scale(Scale);
            Controller?.Render(canvas, Scale, region);
        }

        using (new SKAutoCanvasRestore(canvas))
        {
            canvas.Translate(Offset);
            canvas.Scale(Scale);
            if (selecting)
            {
                using SKPaint paint = new();
                paint.StrokeWidth = 2;
                paint.PathEffect = SKPathEffect.CreateDash([7, 3], 0);
                paint.Style = SKPaintStyle.Stroke;
                canvas.DrawRect(Selected, paint);
            }
            else
            {
                using SKPaint paint = new();
                paint.Color = SKColors.Coral.WithAlpha(128);
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRect(Selected, paint);
            }
        }
    }
}