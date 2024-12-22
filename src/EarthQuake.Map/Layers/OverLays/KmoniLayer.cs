using EarthQuake.Core;
using EarthQuake.Core.Animation;
using SkiaSharp;

namespace EarthQuake.Map.Layers.OverLays;

public class KmoniLayer : ForeGroundLayer
{
    private readonly List<EewPoint> points = [];
    public InterpolatedWaveData? Wave { get; set; }

    public override void Render(SKCanvas canvas, float scale, SKRect bounds)
    {
        using SKPaint paint = new();
        paint.IsAntialias = true;
        if (Wave is null) return;
        lock (points)
        {
            // なんか別スレッドから呼ばれることがあるのでロックする
            foreach (var point in points)
            {
                var elapsed = (float)(DateTime.UtcNow - point.Issued).TotalSeconds;
                if (elapsed < 0) continue;
                var hypo = GeomTransform.Translate(point.Point);
                SKPoint center = new(hypo.X * scale, hypo.Y * scale);

                using (new SKAutoCanvasRestore(canvas))
                {
                    {
                        // P波を描画する
                        var radius = Wave.GetPRadius(point.Depth, elapsed);
                        paint.Color = SKColors.SkyBlue;
                        paint.IsStroke = true;
                        paint.StrokeWidth = 2;
                        using SKPath path = new();
                        for (var i = 0; i < 360; i += 10)
                        {
                            if (i == 0)
                            {
                                var point2 = GeomTransform.Translate(point.Point + new SKPoint(0, radius));
                                path.MoveTo(point2.X * scale, point2.Y * scale);
                            }
                            else
                            {
                                var (sin, cos) = Math.SinCos(i * double.Pi / 180);
                                var point2 =
                                    GeomTransform.Translate(point.Point +
                                                            new SKPoint((float)sin * radius, (float)cos * radius));
                                path.LineTo(point2.X * scale, point2.Y * scale);
                            }
                        }

                        path.Close();
                        canvas.DrawPath(path, paint);
                    }
                    {
                        var color = SKColors.Red.WithAlpha(120);
                        // S波を描画する
                        var radius = Wave.GetSRadius(point.Depth, elapsed);
                        paint.Shader = SKShader.CreateRadialGradient(
                            center, // 中心座標
                            radius * scale * 50, // 円の半径
                            [SKColors.Transparent, color], // 色
                            [0, 1], // 色の位置
                            SKShaderTileMode.Clamp); // タイルモード
                        paint.IsStroke = false;
                        using SKPath path = new();
                        for (var i = 0; i < 360; i += 10)
                        {
                            if (i == 0)
                            {
                                var point2 = GeomTransform.Translate(point.Point + new SKPoint(0, radius));
                                path.MoveTo(point2.X * scale, point2.Y * scale);
                            }
                            else
                            {
                                var (sin, cos) = Math.SinCos(i * double.Pi / 180);
                                var point2 =
                                    GeomTransform.Translate(point.Point +
                                                            new SKPoint((float)sin * radius, (float)cos * radius));
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

        foreach (var center in from point in points let elapsed = (DateTime.UtcNow - point.Issued).TotalSeconds let hypo = GeomTransform.Translate(point.Point) let center = new SKPoint(hypo.X * scale, hypo.Y * scale) where elapsed > 0 && elapsed % 1 < 0.5 select center)
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

    private protected override void Initialize()
    {
    }

    public void SetHypo((float lat, float lon) point, DateTime issued, int depth)
    {
        points.Add(new EewPoint(new SKPoint(point.lon, point.lat), issued, depth));
    }

    private record EewPoint(SKPoint Point, DateTime Issued, int Depth);
}