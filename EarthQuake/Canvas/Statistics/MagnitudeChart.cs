using System;
using System.Linq;
using EarthQuake.Core.EarthQuakes;
using EarthQuake.Map.Layers;
using SkiaSharp;

namespace EarthQuake.Canvas.Statistics;

public class MagnitudeChart : StatisticsCanvas
{

    private protected override void Render(SKCanvas canvas)
    {
        var data = Epicenters.OrderBy(x => x.Properties.Mag ?? 0).ToList();
        var min = data.Min(x => x.Properties.Date.Date.Ticks);
        var max = data.Max(x => x.Properties.Date.Date.Ticks);
        using SKPaint paint = new();
        paint.Color = SKColors.Gray;
        paint.Typeface = MapLayer.Font;
        paint.TextSize = 6;
        paint.IsAntialias = true;

        var height = (float)Bounds.Height;
        var width = (float)Bounds.Width;

        var rangeLong = max - min + TimeSpan.TicksPerDay;
        float range = rangeLong;
        var mMax = data.Last().Properties.Mag ?? 0;
        var heightMax = MathF.Ceiling(mMax);
        paint.Color = SKColors.Pink;
        var totalHour = (int)(rangeLong / TimeSpan.TicksPerHour) * 4;
        var total = new int[totalHour];
        foreach (var item in data)
        {
            var x = (item.Properties.Date.Ticks - min) / range * width;
            var mg = item.Properties.Mag ?? 0;
            var y = mg / heightMax * height;
            canvas.DrawLine(x, height - y, x, (float)Bounds.Height, paint);
            total[(int)((item.Properties.Date.Ticks - min) / (TimeSpan.TicksPerHour / 4))]++;
        }

        paint.Color = SKColors.Gray;

        for (var i = 1; i <= heightMax; i++)
        {
            var y = i / heightMax * height;
            canvas.DrawLine(0, y, width, y, paint);
            canvas.DrawText($"M{heightMax - i}.0", 0, y, paint);
        }

        for (long i = 0; i < range; i += TimeSpan.TicksPerHour * 3 * (rangeLong / TimeSpan.TicksPerDay))
        {
            var x = i * width / range;
            canvas.DrawLine(x, 0, x, height, paint);
            canvas.DrawText(new DateTime(min + i).ToString("d日 HH時"), x, height - 10, paint);
        }

        var sum = total.Sum();
        paint.Color = SKColors.DimGray;
        paint.StrokeWidth = 1;
        canvas.DrawLine(0, height, width / totalHour, height - total[0] * height / sum, paint);
        var itemTotal = total[0];
        for (var i = 1; i < totalHour; i++)
        {
            canvas.DrawLine(i * width / totalHour, height - itemTotal * height / sum,
                (i + 1) * width / totalHour, height - (itemTotal + total[i]) * height / sum, paint);
            itemTotal += total[i];
        }

        var today = DateTime.Now.Date;
        {
            var item = data.Last();
            var x = (item.Properties.Date.Ticks - min) / range * width;
            var y = Math.Max(7, height - (item.Properties.Mag ?? 0) / heightMax * height);
            paint.TextSize = 7;
            var text = $"'{item.Properties.Date:yy/M/d HH:mm:ss}発生";
            var textWidth = paint.MeasureText(text);
            paint.TextAlign = x + textWidth > Bounds.Width ? SKTextAlign.Right : SKTextAlign.Left;
            canvas.DrawText($"最大値 M{item.Properties.Mag:0.0}", x, y, paint);
            canvas.DrawText(text, x, y + 7, paint);
            canvas.DrawText(
                item.Properties.Si == ' '
                    ? item.Properties.Date.Date == today ? "震度情報なし" : "（無感地震）"
                    : "最大震度:" + item.Properties.Scale.ToScreenString(), x, y + 14, paint);
            paint.TextAlign = SKTextAlign.Right;
            canvas.DrawText(sum.ToString(), width, 7, paint);
        }
    }
}