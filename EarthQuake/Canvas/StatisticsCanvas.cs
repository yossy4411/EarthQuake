using Avalonia;
using Avalonia.Media;
using EarthQuake.Core.EarthQuakes;
using EarthQuake.Core.GeoJson;
using EarthQuake.Map.Colors;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EarthQuake.Map.Layers;


namespace EarthQuake.Canvas
{
    public class StatisticsCanvas : SkiaCanvasView
    {
        public enum StatisticType : byte
        {
            EpicentersDepth = 0,
            Magnitudes,
            QuakeScales,
        }

        public StatisticType Type { get; set; }

        public List<Epicenters.Epicenter> Epicenters { get => epicenters; set{ epicenters = value; buffer = null; } }
        private List<Epicenters.Epicenter> epicenters = [];
        private object? buffer;

        public static readonly DirectProperty<StatisticsCanvas, StatisticType> TypeProperty = AvaloniaProperty.RegisterDirect<StatisticsCanvas, StatisticType>(nameof(Type), o => o.Type, (o, v) => o.Type = v);
        public override void Render(ImmediateDrawingContext context)
        {
            using var lease = GetSKCanvas(context);
            if (lease is null) return;
            var canvas = lease.SkCanvas;
            using SKPaint paint = new();
            paint.Color = SKColors.Gray;
            paint.Typeface = MapLayer.Font;
            paint.TextSize = 6;
            canvas.ClipRect(new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height));
            canvas.Clear(SKColors.Black);
            var height = (float)Bounds.Height;
            var width = (float)Bounds.Width;
            if (Epicenters.Any())
            {
                switch (Type)
                {
                    case StatisticType.EpicentersDepth:
                    {
                        double minX, rangeX;
                        double maxY, rangeY;
                        {
                            (double min, double _, double delta, double range) = CalculateOffset(Epicenters, x => x.Geometry.Coordinates[0]);
                            for (double i = 0; i <= range; i += delta)
                            {
                                var x = (float)(i * (width - 50) / range);
                                canvas.DrawLine(x, 0, x, height, paint);
                                canvas.DrawText("E" + ((int)((i + min) * 10)) * 0.1, x, height - 7, paint);
                            }
                            minX = min;
                            rangeX = range;
                        }
                        {
                            (double _, double max, double delta, double range) = CalculateOffset(Epicenters, x => x.Geometry.Coordinates[1]);
                            for (double i = 0; i <= range; i += delta)
                            {
                                var y = (float)(i * (height - 50) / range) + 50;
                                canvas.DrawLine(0, y, width, y, paint);
                                canvas.DrawText("N" + (int)((max - i) * 10) * 0.1, 0, y, paint);
                            }
                            maxY = max;
                            rangeY = range;
                        }
                        paint.Color = SKColors.Pink;
                        var dmax = Epicenters.Select(x => x.Properties.Dep ?? 0).Max();
                        foreach (var item in Epicenters)
                        {
                            var x = (float)((item.Geometry.Coordinates[0] - minX) * (width - 50) / rangeX);
                            var y = (float)((maxY - item.Geometry.Coordinates[1]) * (height - 50) / rangeY) + 50;
                            canvas.DrawPoint(x, (item.Properties.Dep ?? 0) / dmax * 50, paint);
                            canvas.DrawPoint(width - 50 + (item.Properties.Dep ?? 0) / dmax * 50, y, paint);
                            canvas.DrawPoint(x, y, paint);
                        }
                        break;
                    }
                    case StatisticType.Magnitudes:
                    {
                        List<Epicenters.Epicenter> data;
                        long min, max;

                        if (buffer is (List<Epicenters.Epicenter>, long, long))
                        {
                            var buffer2 = ((List<Epicenters.Epicenter>, long, long))buffer;
                            data = buffer2.Item1.ToList();
                            min = buffer2.Item2;
                            max = buffer2.Item3;
                        }
                        else
                        {
                            data = Epicenters.OrderBy(x => x.Properties.Mag ?? 0).ToList();
                            min = data.Min(x => x.Properties.Date.Date.Ticks);
                            max = data.Max(x => x.Properties.Date.Date.Ticks);
                            buffer = (data, min, max);
                        }
                        var rangeLong = max - min + TimeSpan.TicksPerDay;
                        float range = rangeLong;
                        var mMax = data.Last().Properties.Mag ?? 0;
                        var heightMax = MathF.Ceiling(mMax);
                        paint.Color = SKColors.Pink;
                        var totalHour = (int)(rangeLong / TimeSpan.TicksPerHour) * 4;
                        var total = new int[totalHour];
                        using SKPaint paint2 = new();
                        paint2.Color = SKColors.White;
                        paint2.IsAntialias = true;
                        paint2.Typeface = MapLayer.Font;
                        paint2.TextSize = 7;
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
                            canvas.DrawLine(i * width / totalHour, height - itemTotal * height / sum, (i + 1) * width / totalHour, height - (itemTotal + total[i]) * height / sum, paint);
                            itemTotal += total[i];
                        }
                        var today = DateTime.Now.Date;
                        {
                            var item = data.Last();
                            var x = (item.Properties.Date.Ticks - min) / range * width;
                            var y = Math.Max(7, height - (item.Properties.Mag ?? 0) / heightMax * height);


                            var text = $"'{item.Properties.Date:yy/M/d HH:mm:ss}発生";
                            var textWidth = paint2.MeasureText(text);
                            paint2.TextAlign = x + textWidth > Bounds.Width ? SKTextAlign.Right : SKTextAlign.Left;
                            canvas.DrawText($"最大値 M{item.Properties.Mag:0.0}", x, y, paint2);
                            canvas.DrawText(text, x, y + 7, paint2);
                            canvas.DrawText(item.Properties.Si == ' '? item.Properties.Date.Date == today ? "震度情報なし" : "（無感地震）" : "最大震度:" + item.Properties.Scale.ToScreenString(), x, y + 14, paint2);
                            paint2.TextAlign = SKTextAlign.Right;
                            canvas.DrawText(sum.ToString(), width, 7, paint2);
                        }
                        break;
                    }
                    case StatisticType.QuakeScales:
                    {
                        List<Epicenters.Epicenter> data;

                        if (buffer is List<Epicenters.Epicenter> buffer2)
                        {
                            data = buffer2;
                        }
                        else
                        {
                            data = Epicenters.Where(x => x.Properties.Scale is not Scale.Unknown).OrderBy(x => x.Properties.Date).ToList();
                            buffer = data;
                        }
                        if (data.Any())
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
                                    path.LineTo(i * width / (length0 - 1),  height - (count2[i, j - 1] * height / max));
                                }
                                paint.Color = Core.EarthQuakes.Converter.FromInt(j).GetKiwi3Color();
                                path.Close();
                                canvas.DrawPath(path, paint);
                            }
                        }
                        else
                        {
                            paint.Color = SKColors.White;
                            paint.TextSize = 15;
                            paint.TextAlign = SKTextAlign.Center;
                            paint.IsAntialias = true;
                            canvas.DrawText("震度１以上の地震なし", width / 2, height / 2, paint);
                        }
                        break;
                    }
                    default:
                    {
                        Debug.WriteLine("Unknown StatisticType");
                        break;
                    }
                }
            }
            else
            {
                paint.Color = SKColors.White;
                paint.TextSize = 15;
                paint.TextAlign = SKTextAlign.Center;
                paint.IsAntialias = true;
                canvas.DrawText("データなし", width / 2, height / 2, paint);
            }
        }
        private static (float, float, float, float) CalculateOffset(List<Epicenters.Epicenter> floats, Func<Epicenters.Epicenter, float> func)
        {
            var max = floats.Max(func);
            var min = floats.Min(func);
            var a = (int)Math.Max(-1, Math.Floor(Math.Log10(max - min)));
            var delta = Pow(10, a);
            var min2 = MathF.Floor(min / delta) * delta;
            var max2 = MathF.Floor(max / delta) * delta + delta;
            return (min2, max2, delta, max2 - min2);
        }
        private static float Pow(int a, int b)
        {
            if (b < 0) return 1f / a;
            var v = 1;
            for (var i = 0; i < b; i++)
            {
                v *= a;
            }
            return v;
        }
    }
}
