using EarthQuake.Core;
using SkiaSharp;

namespace EarthQuake.Map.Layers
{
    public class MapTilesLayer(string source) : MapLayer
    {
        private readonly MapTilesController _controller = new(source);
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            if (_controller.Transform is null) return;
            
            var origin = _controller.Transform.TranslateToNonTransform(bounds.Left, bounds.Top);
            MapTilesController.GetXYZTile(origin, (int)Math.Log2(scale) + 6, out var point);
            var zoom = (int)Math.Pow(2, point.Z);
            var h = (int)Math.Ceiling(bounds.Height / GeomTransform.Zoom / (GeomTransform.Height * 2f / zoom));
            var w = (int)Math.Ceiling(bounds.Width / GeomTransform.Zoom / (360f / zoom));
            h = Math.Min(h, zoom - point.Y);
            w = Math.Min(w, zoom - point.X);
            for (var j = 0; j <= h; j++)
            {
                
                for (var i = 0; i <= w; i++)
                {
                    if (!_controller.TryGetTile(point.Add(i, j), out var tile) || tile!.Image is null) continue;
                    using (new SKAutoCanvasRestore(canvas))
                    {
                        var resizeX = 360f * GeomTransform.Zoom / MapTilesController.ImageSize / tile.Zoom;
                        var resizeY = (float)(GeomTransform.Height * 2) * GeomTransform.Zoom / MapTilesController.ImageSize / tile.Zoom;
                        canvas.Scale(resizeX, resizeY);
                        canvas.DrawBitmap(tile.Image, tile.LeftTop.X / resizeX, tile.LeftTop.Y / resizeY);
                    }
                }
            }

        }

        private protected override void Initialize(GeomTransform geo)
        {
            _controller.Transform = geo;
        }
    }
}
