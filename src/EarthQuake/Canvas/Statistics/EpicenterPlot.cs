using System;
using System.Linq;
using EarthQuake.Map.Layers;
using SkiaSharp;

namespace EarthQuake.Canvas.Statistics;

public class EpicenterPlot : StatisticsCanvas
{
    private protected override void Render(SKCanvas canvas)
    {
        using SKPaint paint = new();
        paint.Color = SKColors.Gray;
        paint.Typeface = MapLayer.Font;
        paint.TextSize = 6;
        paint.IsAntialias = true;

        var height = (float)Bounds.Height;
        var width = (float)Bounds.Width;
        CalculateOffset(Epicenters.Min(x => x.Geometry.Coordinates[0]),
                        Epicenters.Max(x => x.Geometry.Coordinates[0]), width - 50, 50, out var deltaX, out var minX,
                        out var maxX, out var count);
        for (var i = 0; i <= count; i ++)
        {
            var x = i * (width - 70) / count + 20;
            canvas.DrawLine(x, 0, x, height, paint);
            canvas.DrawText($"E{i * deltaX + minX:F2}", x, height - 6, paint);
        }
        var rangeX = maxX - minX;
        CalculateOffset(Epicenters.Min(x => x.Geometry.Coordinates[1]),
            Epicenters.Max(x => x.Geometry.Coordinates[1]), height - 50, 50, out var deltaY, out var minY,
            out var maxY, out count);
        for (var i = 0; i <= count; i ++)
        {
            var y = i * (height - 70) / count + 50;
            canvas.DrawLine(0, y, width, y, paint);
            canvas.DrawText($"N{i * deltaY + minY:F2}", 0, y, paint);
        }
        var rangeY = maxY - minY;
        
        paint.StrokeWidth = 1;
        var dMax = Epicenters.Select(x => x.Properties.Dep ?? 0).Max();
        foreach (var item in Epicenters)
        {
            var x = (item.Geometry.Coordinates[0] - minX) * (width - 70) / rangeX + 20;
            var y = (maxY - item.Geometry.Coordinates[1]) * (height - 70) / rangeY + 50;
            byte alpha = 255; // デバッグのためにコメントアウト
            /*item.Properties.Mag switch
            {
                >= 5 => 255,
                >= 4 => 200,
                >= 3 => 150,
                >= 2 => 100,
                >= 1 => 50,
                _ => 25
            };*/
            paint.Color = SKColors.Pink.WithAlpha(alpha);
            canvas.DrawPoint(x, (item.Properties.Dep ?? 0) / dMax * 50, paint);
            canvas.DrawPoint(width - 50 + (item.Properties.Dep ?? 0) / dMax * 50, y, paint);
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
        count = (int)((end - start) / delta);
    }
}