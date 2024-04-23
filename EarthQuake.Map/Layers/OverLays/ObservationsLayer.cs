using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Core.EarthQuakes;
using EarthQuake.Core;
using EarthQuake.Map.Colors;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Map.Layers.OverLays
{
    /// <summary>
    /// 観測点データを表示するためのレイヤー
    /// </summary>
    public class ObservationsLayer : ForeGroundLayer
    {
        private SKPoint hypo;
        private SKPoint[]? opoints;
        private Scale[]? ocolors;
        public IList<Station>? Stations { get; set; }
        private bool _drawStations = false;
        public bool DrawStations { get => _drawStations; set { _drawStations = value; } }
        public static SKPath HypoPath 
        { 
            get
            {
                SKPoint[] points =
                [
                    ..new[]
                    {
                        new SKPoint(-4, 3),
                        new SKPoint(-3, 4),
                        new SKPoint(0, 1),
                        new SKPoint(3, 4),
                        new SKPoint(4, 3),
                        new SKPoint(1, 0),
                        new SKPoint(4, -3),
                        new SKPoint(3, -4),
                        new SKPoint(0, -1),
                        new SKPoint(-3, -4),
                        new SKPoint(-4, -3),
                        new SKPoint(-1, 0)
                    }.Select(x => new SKPoint(x.X * 3, x.Y * 3))
                ];
                SKPath path = new();
                path.AddPoly(points);
                return path;
            } 
        }
        private protected override void Initialize(GeomTransform geo)
        {
        }
        public void SetData(PQuakeData quakeData, GeomTransform geo)
        {
            if (quakeData is PQuakeData data)
            {
                if (data.EarthQuake.Hypocenter != null)
                {
                    hypo = geo.Translate(data.EarthQuake.Hypocenter.Longitude, data.EarthQuake.Hypocenter.Latitude);
                }
                if (data.Points is not null && Stations is not null)
                {
                    opoints = data.Points.Select(x => Stations.FirstOrDefault(v => v.Name == x.Addr)?.GetSKPoint(geo) ?? new()).ToArray();
                    ocolors = data.Points.Select(x => x.Scale).ToArray();
                }
            }
        }
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            SKPoint scale2 = new(hypo.X * scale, hypo.Y * scale);
            using SKPaint paint = new() { Color = SKColors.White, IsStroke = true, StrokeWidth = 5, IsAntialias = true };
            using (new SKAutoCanvasRestore(canvas))
            {
                canvas.Translate(scale2);
                canvas.DrawPath(HypoPath, paint);
                paint.IsStroke = false;
                paint.Color = SKColors.Red;
                canvas.DrawPath(HypoPath, paint);
            }
            paint.StrokeWidth = 4;
            if (opoints is not null && ocolors is not null && _drawStations)
            {
                for (int i = opoints.Length - 1; i >= 0; i--)
                {
                    SKPoint item = opoints[i];
                    SKColor color = Kiwi3Color.GetColor(ocolors[i]);
                    paint.Color = color.IncreaseBrightness(-30);
                    paint.Style = SKPaintStyle.Stroke;
                    canvas.DrawCircle(item.X * scale, item.Y * scale, 9, paint);
                    paint.Color = color;
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawCircle(item.X * scale, item.Y * scale, 7, paint);
                }
            }
        }
    }
}
