using Avalonia.Media;
using EarthQuake.Core;
using EarthQuake.Core.Animation;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Map.Layers.OverLays
{
    public class KmoniLayer : ForeGroundLayer
    {
        private GeomTransform? transform;
        private readonly List<EewPoint> points = [];
        public PSWave? Wave { get; set; }
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            using SKPaint paint = new() { IsAntialias = true };
            foreach (EewPoint point in points)
            {
                
                double elapsed = (DateTime.Now - point.Issued).TotalSeconds;
                if (transform is not null)
                {
                    SKPoint hypo = transform.Translate(point.Point);
                    SKPoint center = new(hypo.X * scale, hypo.Y * scale);
                    using (new SKAutoCanvasRestore(canvas))
                    {
                        {
                            // P波を描画する
                            float radius = (float)Wave![point.Depth].GetRadius(elapsed);
                            paint.Color = SKColors.SkyBlue;
                            paint.IsStroke = true;
                            paint.StrokeWidth = 2;
                            using SKPath path = new();
                            for (int i = 0; i < 360; i += 10)
                            {
                                if (i == 0)
                                {
                                    SKPoint point2 = transform.Translate(point.Point + new SKPoint(0, radius));
                                    path.MoveTo(point2.X * scale, point2.Y * scale);
                                }
                                else
                                {
                                    (double sin, double cos) = Math.SinCos(i * double.Pi / 180);
                                    SKPoint point2 = transform.Translate(point.Point + new SKPoint((float)sin * radius, (float)cos * radius));
                                    path.LineTo(point2.X * scale, point2.Y * scale);
                                }
                            }
                            path.Close();
                            canvas.DrawPath(path, paint);
                        }
                        {
                            SKColor color = SKColors.Red.WithAlpha(120);
                            // S波を描画する
                            float radius = (float)Wave![1000 + point.Depth].GetRadius(elapsed);
                            paint.Shader = SKShader.CreateRadialGradient(
                                center,       // 中心座標
                                radius * scale * 50,  // 円の半径
                                [SKColors.Transparent, color],       // 色
                                [0, 1],     // 色の位置
                                SKShaderTileMode.Clamp); // タイルモード
                            paint.IsStroke = false;
                            using SKPath path = new();
                            for (int i = 0; i < 360; i += 10)
                            {
                                if (i == 0)
                                {
                                    SKPoint point2 = transform.Translate(point.Point + new SKPoint(0, radius));
                                    path.MoveTo(point2.X * scale, point2.Y * scale);
                                }
                                else
                                {
                                    (double sin, double cos) = Math.SinCos(i * double.Pi / 180);
                                    SKPoint point2 = transform.Translate(point.Point + new SKPoint((float)sin * radius, (float)cos * radius));
                                    path.LineTo(point2.X * scale, point2.Y * scale);
                                }
                            }
                            path.Close();
                            canvas.DrawPath(path, paint);
                            paint.Shader = null;
                            paint.IsStroke = true;
                            paint.Color = color.WithAlpha(200);
                            canvas.DrawPath(path, paint);
                        }
                    }
                }
            }
            foreach (var point in points)
            {
                if (transform is not null)
                {
                    double elapsed = (DateTime.Now - point.Issued).TotalSeconds;
                    SKPoint hypo = transform.Translate(point.Point);
                    SKPoint center = new(hypo.X * scale, hypo.Y * scale);
                    if (elapsed > 0 && elapsed % 1 < 0.5)
                    {
                        using (new SKAutoCanvasRestore(canvas))
                        {
                            paint.Color = SKColors.White;
                            paint.IsStroke = true;
                            paint.StrokeWidth = 5;
                            canvas.Translate(center);
                            canvas.DrawPath(ObservationsLayer.HypoPath, paint);
                            paint.IsStroke = false;
                            paint.Color = SKColors.Red;
                            canvas.DrawPath(ObservationsLayer.HypoPath, paint);
                        }
                    }
                }
            }
        }

        private protected override void Initialize(GeomTransform geo)
        {
            transform = geo;
/*            SetHypo((33.137551f, 135.050537f), DateTime.Now.AddSeconds(3), 30);
            SetHypo((38.307181f, 142.708f), DateTime.Now.AddSeconds(3), 20);
            SetHypo((37.485320f, 137.25f), DateTime.Now.AddSeconds(3), 0);*/
            SetHypo((33.317676f, 134.868164f), DateTime.Now.AddSeconds(3), 20);
            SetHypo((33.179859f, 133.567383f), DateTime.Now.AddSeconds(25), 30);
            SetHypo((34.521041f, 137.304932f), DateTime.Now.AddSeconds(45), 20);
            SetHypo((35.306608f, 138.509033f), DateTime.Now.AddSeconds(58), 20);
            SetHypo((32.674523f, 132.431395f), DateTime.Now.AddSeconds(47), 40);
            SetHypo((31.864496f, 131.886472f), DateTime.Now.AddSeconds(68), 30);
        }
        public void SetHypo((float lat, float lon) point, DateTime issued, int depth)
        {
            points.Add(new(new SKPoint(point.lon, point.lat), issued, depth));
        }
        private record EewPoint(SKPoint Point, DateTime Issued, int Depth);
    }
}
