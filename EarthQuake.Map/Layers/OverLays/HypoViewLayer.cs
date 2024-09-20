using EarthQuake.Core;
using EarthQuake.Core.GeoJson;
using SkiaSharp;


namespace EarthQuake.Map.Layers.OverLays;

/// <summary>
/// 震央の分布を表示するためのレイヤー。
/// </summary>
public class HypoViewLayer : MapLayer
{
    private readonly List<Epicenter> points = [];
    public override void Render(SKCanvas canvas, float scale, SKRect selected)
    {
        using SKPaint paint = new();
            
        foreach (var (_, p, magnitude) in points)
        {
            var radius = 1 + magnitude is null ? 0 : float.Pow(1.4f, (float)magnitude) / scale * 2.2f;
            paint.Color = SKColor.FromHsv(p.Z, 100, 100);
            paint.Style = SKPaintStyle.Stroke;
            canvas.DrawCircle(p.X, p.Y, radius, paint);

            paint.Color = paint.Color.WithAlpha(100);
            paint.Style = SKPaintStyle.Fill;
            canvas.DrawCircle(p.X, p.Y, radius, paint);
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