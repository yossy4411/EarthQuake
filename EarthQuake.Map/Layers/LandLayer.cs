using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Core.TopoJson;
using EarthQuake.Map.Colors;
using SkiaSharp;
using EarthQuake.Map.Tiles.File;

namespace EarthQuake.Map.Layers
{
    public class LandLayer(PolygonsSet? polygons, string layerName) : CacheableLayer
    {
        public bool Draw { get; set; } = true;
        private SKColor[]? colors;
        private readonly string[]? names = polygons?.Filling[layerName].Names;

        private FileTilesController? fileTilesController;
        public bool AutoFill { get; init; }
        private int scale = -1;
        private protected override void Initialize()
        {
            fileTilesController = polygons is null
                ? null
                : new FileTilesController(polygons, layerName)
                {
                    OnUpdate = HandleUpdated
                };
        }
        public void SetInfo(PQuakeData quakeData)
        {
            fileTilesController.ClearCaches();
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
            scale = -1;
            HandleUpdated();
        }
        public void Reset()
        {
            colors = null;
        }
        public static int GetIndex(float scale)
        => Math.Max(0, Math.Min((int)(-Math.Log(scale * 2, 3) + 3.3), 5));

        public override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            // 表示テスト
            var zoom = GetIndex(scale);
            if (!Draw) return;
            using SKPaint paint = new();
            paint.IsAntialias = true;
            paint.Style = SKPaintStyle.Fill;
            if (fileTilesController is null) return;
            if (colors is null) return;
            for (var i = 0; i < colors.Length; i++)
            {
                paint.Color = colors[i];
                if (paint.Color == SKColors.Empty) continue;
                var tile = fileTilesController.TryGetTile(zoom, i);
                if (tile is not null) canvas.DrawVertices(tile, SKBlendMode.Src, paint);
            }
        }

        public override bool IsReloadRequired(float zoom, SKRect bounds)
        {
            if (scale == GetIndex(zoom)) return false;
            scale = GetIndex(zoom);
            return true;
        }
    }
}
