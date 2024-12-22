using System;
using System.Collections.Generic;
using System.Linq;
using EarthQuake.Core.GeoJson;
using EarthQuake.Map.Layers;
using SkiaSharp;

namespace EarthQuake.Canvas.Statistics;

public class EpicenterPlot : StatisticsCanvas
{
    private readonly List<(Epicenters.Epicenter, SKPoint)> cache = [];
    private float _minX, _maxY, _rangeX, _rangeY;
    private float _maxDepth;
    private protected override void Render(SKCanvas canvas)
    {
        cache.Clear();
        using SKPaint paint = new();
        paint.Color = SKColors.Gray;
        paint.Typeface = MapLayer.Font;
        paint.TextSize = 6;
        paint.IsAntialias = true;

        var height = (float)Bounds.Height;
        var width = (float)Bounds.Width;
        var offsetX = (float)Bounds.Left;
        var offsetY = (float)Bounds.Top;
        CalculateOffset(Epicenters.Min(x => x.Geometry.Coordinates[0]),
                        Epicenters.Max(x => x.Geometry.Coordinates[0]), width - 50, 50, out var deltaX, out var minX,
                        out var maxX, out var count);
        
        for (var i = 0; i <= count; i ++)
        {
            var x = offsetX + i * (width - 70) / count + 20;
            canvas.DrawLine(x, offsetY, x, offsetY + height, paint);
            canvas.DrawText($"E{i * deltaX + minX:F2}", x, offsetY + height - 6, paint);
        }
        var rangeX = maxX - minX;
        _minX = minX;
        _rangeX = rangeX;
        
        CalculateOffset(Epicenters.Min(x => x.Geometry.Coordinates[1]),
                        Epicenters.Max(x => x.Geometry.Coordinates[1]), height - 50, 50, out var deltaY, out var minY,
            out var maxY, out count);
        for (var i = 0; i <= count; i ++)
        {
            var y = offsetY + i * (height - 70) / count + 50;
            canvas.DrawLine(offsetX, y, offsetX + width, y, paint);
            canvas.DrawText($"N{maxY - i * deltaY:F2}", offsetX, y, paint);
        }
        var rangeY = maxY - minY;
        _maxY = maxY;
        _rangeY = rangeY;
        
        paint.StrokeWidth = 1;
        var maxDepth = MathF.Ceiling(Epicenters.Select(x => x.Properties.Dep ?? 0).Max());
        _maxDepth = maxDepth;
        foreach (var item in Epicenters)
        {
            var x = offsetX + (item.Geometry.Coordinates[0] - minX) / rangeX * (width - 70) + 20;
            var y = offsetY + (maxY - item.Geometry.Coordinates[1]) / rangeY * (height - 70) + 50;
            cache.Add((item, new SKPoint(x, y)));
            byte alpha = item.Properties.Mag switch
            {
                >= 5 => 255,
                >= 4 => 200,
                >= 3 => 150,
                >= 2 => 100,
                >= 1 => 50,
                _ => 25
            };
            paint.Color = SKColors.Pink.WithAlpha(alpha);
            canvas.DrawPoint(x, offsetY + (item.Properties.Dep ?? 0) / maxDepth * 50, paint);
            canvas.DrawPoint(width - 50 - offsetX + (item.Properties.Dep ?? 0) / maxDepth * 50, y, paint);
            canvas.DrawPoint(x, y, paint);
        }
    }
    
    /// <summary>
    /// 描画サイズを計算します。
    /// </summary>
    /// <param name="min">値の最小値</param>
    /// <param name="max">値の最大値</param>
    /// <param name="size">サイズ（画面座標）</param>
    /// <param name="preferredSize">好ましい間隔（画面座標）</param>
    /// <param name="delta">計算された間隔（画面座標）</param>
    /// <param name="start">初期値（値）</param>
    /// <param name="end">最終値（値）</param>
    /// <param name="count">描画回数</param>
    private static void CalculateOffset(float min, float max, float size, float preferredSize, out float delta, out float start, out float end, out int count)
    {
        var range = max - min;
        var preferredCount = MathF.Ceiling(size / preferredSize);
        var preferredDelta = range / preferredCount;
        // 10の累乗数の範囲で最も近い整数を求める
        var pow = Math.Pow(10, Math.Floor(Math.Log10(preferredDelta)));
        var delta1 = (float)(Math.Ceiling(preferredDelta / pow) * pow);
        var delta2 = (float)(Math.Floor(preferredDelta / pow) * pow);
        delta = Math.Abs(delta1 - preferredDelta) < Math.Abs(delta2 - preferredDelta) ? delta1 : delta2;
        start = (float)Math.Floor(min / delta) * delta;
        end = (float)Math.Ceiling(max / delta) * delta;
        count = (int)Math.Round((end - start) / delta);
    }

    private protected override void RenderOverlay(SKCanvas canvas, SKPoint mousePoint)
    {
        base.RenderOverlay(canvas, mousePoint);
        using SKPaint paint = new();
        paint.Color = SKColors.Gray;
        paint.Typeface = MapLayer.Font;
        paint.TextSize = 6;
        paint.IsAntialias = true;
        var height = (float)Bounds.Height;
        var width = (float)Bounds.Width;
        var offsetX = (float)Bounds.Left;
        var offsetY = (float)Bounds.Top;
        
        // 一番マウスに近い点を探す
        var (item, point) = cache.OrderBy(tuple => SKPoint.DistanceSquared(tuple.Item2, mousePoint)).First();
        if (SKPoint.Distance(point, mousePoint) > 3)
        {
            paint.PathEffect = SKPathEffect.CreateDash([5, 5], 0);
            canvas.DrawLine(0, mousePoint.Y, width, mousePoint.Y, paint);
            canvas.DrawLine(mousePoint.X, 0, mousePoint.X, height, paint);
            paint.Color = SKColors.White;
            canvas.DrawText(
                mousePoint.Y < 50 + offsetY
                    ? $"D{_maxDepth - (50 + offsetY - mousePoint.Y) / 50 * _maxDepth:F2}"
                    : $"N{_maxY - (mousePoint.Y - 50 - offsetY) / (height - 70) * _rangeY:F4}", offsetX, mousePoint.Y, paint);

            if (mousePoint.X > width - 50 + offsetX)
            {
                canvas.DrawText(
                    $"D{(mousePoint.X - width + 50 + offsetX) / 50 * _maxDepth:00.0}",
                    mousePoint.X - 17, height - 6, paint);
            }
            else
            {
                canvas.DrawText(
                    $"E{(mousePoint.X - 20 - offsetX) / (width - 70) * _rangeX + _minX:F4}",
                    mousePoint.X, height - 6, paint);
            }

            return;
        }
        paint.Color = SKColors.DodgerBlue.WithAlpha(200);
        paint.TextSize = 7;
        paint.TextAlign = SKTextAlign.Left;
        var text = $"N{item.Geometry.Coordinates[0]:0.000} E{item.Geometry.Coordinates[1]:0.000}";
        var textWidth = paint.MeasureText(text);
        var offset = Math.Min(5, width - mousePoint.X - textWidth - 10);
        canvas.DrawRect(mousePoint.X + offset, mousePoint.Y - 20, textWidth + 10, 20, paint);
        paint.Color = SKColors.White;

        canvas.DrawText($"M{item.Properties.Mag:F1} D{item.Properties.Dep:F1}km", mousePoint.X + 5 + offset, mousePoint.Y - 3, paint);
        canvas.DrawText(text, mousePoint.X + 5 + offset, mousePoint.Y - 10, paint);
        
        paint.Color = SKColors.Gray;
        paint.PathEffect = SKPathEffect.CreateDash([5, 5], 0);
        canvas.DrawLine(0, point.Y, width, point.Y, paint);
        canvas.DrawLine(point.X, 0, point.X, height, paint);

        paint.PathEffect = null;
        paint.Color = SKColors.Red;
        paint.StrokeWidth = 2;
        canvas.DrawPoint(point, paint);
    }
}