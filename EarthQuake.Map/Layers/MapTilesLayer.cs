using EarthQuake.Core;
using SkiaSharp;
using System.Diagnostics;

namespace EarthQuake.Map.Layers
{
    public class MapTilesLayer(string source) : MapLayer
    {
        private readonly MapTilesController _controller = new(source);
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            if (_controller.Transform is null) return;
            
            SKPoint origin = _controller.Transform.TranslateToNonTransform(bounds.Left, bounds.Top);
            MapTilesController.GetXYZTile(origin, (int)Math.Log2(scale) + 6, out MapTilesController.TilePoint point);
            int zoom = (int)Math.Pow(2, point.Z);
            int h = (int)(bounds.Height / _controller.Transform.Zoom / (GeomTransform.Height * 2f / zoom)) + 2;
            int w = (int)(bounds.Width / _controller.Transform.Zoom / (360f / zoom)) + 2;
            h = Math.Min(h, zoom - point.Y);
            w = Math.Min(w, zoom - point.X);
            for (int j = 0; j < h; j++)
            {
                
                for (int i = 0; i < w; i++)
                {
                    if (_controller.TryGetTile(point.Add(i, j), out var tile))
                    {
                        using (new SKAutoCanvasRestore(canvas))
                        {
                            float resizeX = 360f * _controller.Transform.Zoom / 256 / tile!.Zoom;
                            float resizeY = (float)(GeomTransform.Height * 2) * _controller.Transform.Zoom / 256 / tile.Zoom;
                            canvas.Scale(resizeX, resizeY);
                            canvas.DrawBitmap(tile.Image, tile.LeftTop.X / (float)resizeX, tile.LeftTop.Y / (float)resizeY);
                        }
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
