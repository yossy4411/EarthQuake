using Avalonia;
using Avalonia.Media;
using EarthQuake.Core.GeoJson;
using Microsoft.CodeAnalysis.FlowAnalysis;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

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
            using SKFont font = new() { Size = 7 };
            switch (_type)
            {
                case StatisticType.EpicentersDepth:
                    if (Epicenters.Any())
                    {
                        float minX, rangeX;
                        float minY, rangeY;
                        { 
                            (float min, float max, float delta, float range) = CalculateOffset(Epicenters.Select(x => x.Geometry.Coordinates[0]));
                            for (float i = 0; i <= range; i += delta)
                            {
                                float x = (float)(i * (Bounds.Width - 50) / range);
                                canvas.DrawLine(x, 50, x, (float)Bounds.Height, paint);
                                canvas.DrawText(SKTextBlob.Create((i + min).ToString(), font), x, (float)Bounds.Height - 7, paint);
                            }
                            minX = min;
                            rangeX = range;
                        }
                        {
                            (float min, float max, float delta, float range) = CalculateOffset(Epicenters.Select(x => x.Geometry.Coordinates[1]));
                            for (float i = 0; i <= range; i += delta)
                            {
                                float y = (float)(i * (Bounds.Height - 50) / range) + 50 ;
                                canvas.DrawLine(0, y, (float)Bounds.Width - 50, y, paint);
                                var text = SKTextBlob.Create((i + min).ToString(), font);
                                canvas.DrawText(text, 0, y, paint);
                            }
                            minY = min;
                            rangeY = range;
                        }
                        paint.Color = SKColors.Pink;
                        float dmax = Epicenters.Select(x => x.Properties?.Dep ?? 0).Max();
                        foreach (var item in Epicenters)
                        {
                            float x = (float)((item.Geometry.Coordinates[0] - minX) * (Bounds.Width - 50) / rangeX);
                            float y = (float)((item.Geometry.Coordinates[1] - minY) * (Bounds.Height - 50) / rangeX) + 50;
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
            float delta = MathF.Pow(10, Math.Max(-1, MathF.Floor(MathF.Log10(max - min))));
            float min2 = MathF.Floor(min / delta) * delta;
            float max2 = MathF.Floor(max / delta) * delta + delta;
            return (min2, max2, delta, max2 - min2);
        }
    }
}
