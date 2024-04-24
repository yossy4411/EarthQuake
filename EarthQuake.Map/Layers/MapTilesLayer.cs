using EarthQuake.Core;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Map.Layers
{
    public class MapTilesLayer(string source) : MapLayer
    {
        private readonly MapTilesController _controller = new(source);
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            if (_controller.Transform is null) return;
            MapTilesController.TilePoint point;
            {
                if (_controller.TryGetTile(135, 35, (int)Math.Log2(scale) + 5, out var tile, out var tilePoint))
                {
                    using (new SKAutoCanvasRestore(canvas))
                    {
                        float resizeX = 360f * _controller.Transform.Zoom / 256 / tile!.Zoom;
                        float resizeY = (float)(GeomTransform.Height * 2) * _controller.Transform.Zoom / 256 / tile!.Zoom;
                        canvas.Scale(resizeX, resizeY);
                        canvas.DrawBitmap(tile.Image, tile.LeftTop.X / (float)resizeX, tile.LeftTop.Y / (float)resizeY);
                    }
                }
                point = tilePoint;
            }
            /*for(int i = 1; i < 5; i++)
            {
                if (_controller.TryGetTile(point.Add(i, 0), out var tile))
                {
                    using (new SKAutoCanvasRestore(canvas))
                    {
                        float resizeX = 360f * _controller.Transform.Zoom / 256 / tile!.Zoom;
                        float resizeY = (float)(GeomTransform.Height * 2) * _controller.Transform.Zoom / 256 / tile.Zoom;
                        canvas.Scale(resizeX, resizeY);
                        canvas.DrawBitmap(tile.Image, tile.LeftTop.X / (float)resizeX, tile.LeftTop.Y / (float)resizeY);
                    }
                }
            }*/
            
        }

        private protected override void Initialize(GeomTransform geo)
        {
            _controller.Transform = geo;
        }
    }
}
