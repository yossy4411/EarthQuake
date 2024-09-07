using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Core.EarthQuakes;
using EarthQuake.Core;
using EarthQuake.Map.Colors;
using SkiaSharp;

namespace EarthQuake.Map.Layers.OverLays;

/// <summary>
/// 観測点データを表示するためのレイヤー
/// </summary>
public class ObservationsLayer : ForeGroundLayer
{
    private SKPoint hypo;
    private SKPoint[]? oPoints;
    private Scale[]? oColors;
    public IList<Station>? Stations { get; init; }
    public bool DrawStations { get; set; }

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
        if (quakeData.EarthQuake.Hypocenter is not null)
        {
            hypo = GeomTransform.Translate(quakeData.EarthQuake.Hypocenter.Longitude, quakeData.EarthQuake.Hypocenter.Latitude);
        }

        if (quakeData.Points is null || Stations is null) return;
        oPoints = quakeData.Points.Select(x => Stations.FirstOrDefault(v => v.Name == x.Addr)?.GetSKPoint(geo) ?? new()).ToArray();
        oColors = quakeData.Points.Select(x => x.Scale).ToArray();
    }
    internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
    {
        SKPoint scale2 = new(hypo.X * scale, hypo.Y * scale);
        using SKPaint paint = new();
        paint.Color = SKColors.White;
        paint.IsStroke = true;
        paint.StrokeWidth = 5;
        paint.IsAntialias = true;
        using (new SKAutoCanvasRestore(canvas))
        {
            canvas.Translate(scale2);
            canvas.DrawPath(HypoPath, paint);
            paint.IsStroke = false;
            paint.Color = SKColors.Red;
            canvas.DrawPath(HypoPath, paint);
        }
        paint.StrokeWidth = 4;
        if (oPoints is null || oColors is null || !DrawStations) return;
        for (var i = oPoints.Length - 1; i >= 0; i--)
        {
            var item = oPoints[i];
            var color = Kiwi3Color.GetColor(oColors[i]);
            paint.Color = color.IncreaseBrightness(-30);
            paint.Style = SKPaintStyle.Stroke;
            canvas.DrawCircle(item.X * scale, item.Y * scale, 9, paint);
            paint.Color = color;
            paint.Style = SKPaintStyle.Fill;
            canvas.DrawCircle(item.X * scale, item.Y * scale, 7, paint);
        }
    }
}