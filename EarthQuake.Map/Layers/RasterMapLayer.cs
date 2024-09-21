using EarthQuake.Core;
using EarthQuake.Map.Tiles;
using EarthQuake.Map.Tiles.Raster;
using SkiaSharp;

namespace EarthQuake.Map.Layers;

public class RasterMapLayer(string source) : CacheableLayer
{
    private RasterTilesController? _controller;
    private TilePoint _point;
    public override void Render(SKCanvas canvas, float scale, SKRect bounds)
    {
        var origin = GeomTransform.TranslateToNonTransform(bounds.Left, bounds.Top);
        RasterTilesController.GetXyzTile(origin, (int)Math.Log2(scale) + 6, out var point);
        var zoom = (int)Math.Pow(2, point.Z);
        var h = (int)Math.Ceiling(bounds.Height / GeomTransform.Zoom / (GeomTransform.Height * 2f / zoom));
        var w = (int)Math.Ceiling(bounds.Width / GeomTransform.Zoom / (360f / zoom));
        h = Math.Min(h, zoom - point.Y);
        w = Math.Min(w, zoom - point.X);
        for (var j = 0; j <= h; j++)
        {
                
            for (var i = 0; i <= w; i++)
            {
                if (!_controller!.TryGetTile(point.Add(i, j), out var tile) || tile!.Image is null) continue;
                using (new SKAutoCanvasRestore(canvas))
                {
                    var resizeX = 360f * GeomTransform.Zoom / RasterTilesController.ImageSize / tile.Zoom;
                    var resizeY = (float)(GeomTransform.Height * 2) * GeomTransform.Zoom / RasterTilesController.ImageSize / tile.Zoom;
                    canvas.Scale(resizeX, resizeY);
                    canvas.DrawImage(tile.Image, tile.LeftTop.X / resizeX, tile.LeftTop.Y / resizeY);
                }
            }
        }

    }

    private protected override void Initialize()
    {
        _controller = new RasterTilesController(source)
        {
            OnUpdate = HandleUpdated
        };
    }
    public override bool IsReloadRequired(float zoom, SKRect bounds)
    {
        var origin = GeomTransform.TranslateToNonTransform(bounds.Left, bounds.Top);
        RasterTilesController.GetXyzTile(origin, (int)Math.Log2(zoom) + 5, out var point);
        // 表示範囲のタイルが変わったか
        if (_point == point) return false;
        _point = point;
        return true;
    }
}