using Avalonia;
using Avalonia.Media;
using EarthQuake.Core.GeoJson;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;


namespace EarthQuake.Canvas
{
    public class StatisticsCanvas : SkiaCanvasView
    {
        public enum StatisticType
        {
            EpicentersDepth,
            QuakeScales,
        }
        private StatisticType _type;
        public StatisticType Type { get => _type; set => _type = value; }
        public IEnumerable<Epicenters.Epicenter> Epicenters { get; set; } = [];
        
        public static readonly DirectProperty<StatisticsCanvas, StatisticType> TypeProperty = AvaloniaProperty.RegisterDirect<StatisticsCanvas, StatisticType>(nameof(Type), o => o.Type, (o, v) => o.Type = v);
        public override void Render(ImmediateDrawingContext context)
        {
            using var lease = GetSKCanvas(context);
            if (lease is null) return;
            SKCanvas canvas = lease.SkCanvas;
            using SKPaint paint = new() { Color = SKColors.Gray };
            using SKFont font = new() { Size = 7,  };
            switch (_type)
            {
                case StatisticType.EpicentersDepth:
                    if (Epicenters.Any())
                    {
                        double minX, rangeX;
                        double maxY, rangeY;
                        { 
                            (double min, double max, double delta, double range) = CalculateOffset(Epicenters.Select(x => x.Geometry.Coordinates[0]));
                            for (double i = 0; i <= range; i += delta)
                            {
                                float x = (float)(i * (Bounds.Width - 50) / range);
                                canvas.DrawLine(x, 0, x, (float)Bounds.Height, paint);
                                canvas.DrawText(SKTextBlob.Create("E" + (((int)((i + min) * 10)) * 0.1).ToString(), font), x, (float)Bounds.Height - 7, paint);
                            }
                            minX = min;
                            rangeX = range;
                        }
                        {
                            (double min, double max, double delta, double range) = CalculateOffset(Epicenters.Select(x => x.Geometry.Coordinates[1]));
                            for (double i = 0; i <= range; i += delta)
                            {
                                float y = (float)(i * (Bounds.Height - 50) / range) + 50 ;
                                canvas.DrawLine(0, y, (float)Bounds.Width, y, paint);
                                var text = SKTextBlob.Create("N" + (((int)((max - i) * 10)) * 0.1).ToString(), font);
                                canvas.DrawText(text, 0, y, paint);
                            }
                            maxY = max;
                            rangeY = range;
                        }
                        paint.Color = SKColors.Pink;
                        float dmax = Epicenters.Select(x => x.Properties?.Dep ?? 0).Max();
                        foreach (var item in Epicenters)
                        {
                            float x = (float)((item.Geometry.Coordinates[0] - minX) * (Bounds.Width - 50) / rangeX);
                            float y = (float)((maxY - item.Geometry.Coordinates[1]) * (Bounds.Height - 50) / rangeY) + 50;
                            canvas.DrawPoint(x, (item.Properties?.Dep ?? 0) / dmax * 50, paint);
                            canvas.DrawPoint((float)(Bounds.Width - 50) + (item.Properties?.Dep ?? 0) / dmax * 50, y, paint);
                        }
                    }
                    break;
            }
        }
        private static (float, float, float, float) CalculateOffset(IEnumerable<float> floats)
        {
            float max = floats.Max();
            float min = floats.Min();
            int a = (int)Math.Max(-1, Math.Floor(Math.Log10(max - min)));
            float delta = Pow(10, a);
            float min2 = MathF.Floor(min / delta) * delta;
            float max2 = MathF.Floor(max / delta) * delta + delta;
            return (min2, max2, delta, max2 - min2);
        }
        private static float Pow(int a, int b)
        {
            if (b < 0) return 1f / a;
            int v = 1;
            for (int i = 0; i < b; i++)
            {
                v *= a;
            }
            return v;
        }
    }
}
