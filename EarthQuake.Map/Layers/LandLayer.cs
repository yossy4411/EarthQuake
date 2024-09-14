using EarthQuake.Core;
using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Core.TopoJson;
using EarthQuake.Map.Colors;
using SkiaSharp;
using System.Diagnostics;
using EarthQuake.Map.Tiles;

namespace EarthQuake.Map.Layers
{
    public class LandLayer(PolygonsSet? polygons, string layerName) : MapLayer
    {
        public bool Draw { get; set; } = true;
        private SKColor[]? colors;
        private readonly string[]? names = polygons?.Filling[layerName].Names;
        private FileTilesController? fileTilesController = polygons is null ? null : new FileTilesController(polygons, layerName);
        public bool AutoFill { get; init; }
        private protected override void Initialize()
        {
        }
        public void SetInfo(PQuakeData quakeData)
        {
            if (names is null || quakeData.Points is null) return;
            colors = new SKColor[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var a = quakeData.Points.FirstOrDefault(x => x.Addr.StartsWith(name));
                if (a is not null) colors[i] = a.Scale.GetKiwi3Color();
                else if (AutoFill) colors[i] = SKColors.DarkGreen;
                else colors[i] = SKColors.Empty;
            }
        }
        public void Reset()
        {
            colors = null;
        }
        public static int GetIndex(float scale)
        => Math.Max(0, Math.Min((int)(-Math.Log(scale * 2, 3) + 3.3), 5));

        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            // 表示テスト
            // var index = GetIndex(scale);
            if (!Draw) return;
            using SKPaint paint = new();
            var polygon = fileTilesController?.TryGetTile(0, 0);
            if (polygon is null) return;
            paint.IsAntialias = true;
            paint.Color = SKColors.Black;
            paint.Style = SKPaintStyle.Fill;
            canvas.DrawVertices(polygon, SKBlendMode.SrcOver, paint);
        }
        
    }
}
