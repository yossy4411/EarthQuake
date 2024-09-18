using EarthQuake.Core;
using EarthQuake.Core.GeoJson;
using SkiaSharp;


namespace EarthQuake.Map.Layers.OverLays;

/// <summary>
/// 3次元的に震央の分布を表示するためのレイヤー。
/// </summary>
public class Hypo3DViewLayer : ForeGroundLayer
{
    private readonly List<Epicenter> points = [];
    public float Rotation { get; set; }
    public override void Render(SKCanvas canvas, float scale, SKRect selected)
    {
        using SKPaint paint = new();
            
        foreach (var (_, p, magnitude) in points)
        {
                
            if (Rotation == 0)
            {
                var radius = magnitude is null ? 0 : float.Pow(1.7f, (float)magnitude);
                paint.Color = SKColor.FromHsv(p.Z, 100, 100);
                paint.Style = SKPaintStyle.Stroke;
                canvas.DrawCircle(p.X, p.Y, radius, paint);

                if (!selected.Contains(p.X, p.Y)) paint.Color = SKColor.FromHsv(p.Z, 100, 100, 100);
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawCircle(p.X, p.Y, radius, paint);
                   
            }
            else
            {
                using var view = new SK3dView();
                view.RotateXDegrees(Rotation);
                using (new SKAutoCanvasRestore(canvas))
                {
                    view.Save();
                    view.Translate(p.X, -p.Y, p.Z);
                    var radius = magnitude is null ? 0 : float.Pow(1.7f, (float)magnitude);
                    view.ApplyToCanvas(canvas); // 3Dを適用する
                    paint.Color = SKColor.FromHsv(p.Z, 100, 100);
                    paint.Style = SKPaintStyle.Stroke;
                    canvas.DrawCircle(0, 0, radius, paint);
                    
                    if (!selected.Contains(p.X, p.Y)) paint.Color = SKColor.FromHsv(p.Z, 100, 100, 100);
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawCircle(0, 0, radius, paint);
                    
                    view.Restore();
                }
            }
                
        }
        paint.Style = SKPaintStyle.Stroke;
        paint.Color = SKColors.Gray;
            
    }
    public void ClearFeature() => points.Clear();
    public void AddFeature(IEnumerable<Epicenters.Epicenter>? centers)
    {
        if (centers == null) return;
        foreach (var feature in centers.OrderByDescending(x=>x.Properties.Dep??0))
        {
            var p = GeomTransform.Translate(feature.Geometry.Coordinates[0], feature.Geometry.Coordinates[1]);
            points.Add(new Epicenter(feature, new SKPoint3(p.X, p.Y, feature.Properties.Dep??0), feature.Properties.Mag));
        }
            
    }
    private protected override void Initialize()
    {
    }

    public IEnumerable<Epicenters.Epicenter> GetPoints(SKRect rect)
    {
        return points.Where(x => rect.Contains(x.Point.X, x.Point.Y)).Select(x => x.Data);
    }

    private record Epicenter(Epicenters.Epicenter Data, SKPoint3 Point, float? Magnitude);
}