using System;
using System.Linq;
using EarthQuake.Core.EarthQuakes;
using EarthQuake.Map.Colors;
using EarthQuake.Map.Layers;
using SkiaSharp;

namespace EarthQuake.Canvas.Statistics;

public class QuakeScalesGraph : StatisticsCanvas
{
    private protected override void Render(SKCanvas canvas)
    {
        var data = Epicenters.Where(x => x.Properties.Scale is not Scale.Unknown)
                 .OrderBy(x => x.Properties.Date).ToList(); 
        using SKPaint paint = new();
        
        var height = (float)Bounds.Height;
        var width = (float)Bounds.Width;

        if (data.Count != 0)
        {
            var min = data.First().Properties.Date.Date;
            var count = new int[(int)(data.Last().Properties.Date.Date - min).TotalDays + 1, 12];
            foreach (var item in data)
            {
                count[(int)(item.Properties.Date.Date - min).TotalDays, item.Properties.Scale.ToInt()]++;
            }

            var length0 = count.GetLength(0);
            var length1 = count.GetLength(1);
            var count2 = new int[length0, length1];
            var max = 0;
            for (var i = 0; i < length0; i++)
            {
                var a = 0;
                for (var j = 1; j < length1; j++)
                {
                    a += count[i, j];
                    count2[i, j] = a;
                }

                max = Math.Max(max, a);
            }

            for (var j = 1; j < length1; j++)
            {
                using SKPath path = new();
                path.MoveTo(0, height - (count2[0, j] * height / max));
                for (var i = 1; i < length0; i++)
                {
                    path.LineTo(i * width / (length0 - 1), height - (count2[i, j] * height / max));
                }

                for (var i = length0 - 1; i >= 0; i--)
                {
                    path.LineTo(i * width / (length0 - 1), height - (count2[i, j - 1] * height / max));
                }
                paint.Typeface = MapLayer.Font;
                paint.TextSize = 6;
                paint.IsAntialias = true;
                paint.Color = ScaleConverter.FromInt(j).GetKiwi3Color();
                path.Close();
                canvas.DrawPath(path, paint);
            }
        }
        else
        {
            paint.Color = SKColors.White;
            paint.TextSize = 15;
            paint.TextAlign = SKTextAlign.Center;
            paint.Typeface = MapLayer.Font;
            paint.IsAntialias = true;
            canvas.DrawText("震度１以上の地震なし", width / 2, height / 2, paint);
        }
    }
    
}