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
        private readonly string _source = source;
        private GeomTransform? _transform;
        private readonly List<(SKBitmap, SKPoint, float)> images = [];
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            if (_transform is null) return;
            
            
            foreach ((SKBitmap bitmap, SKPoint point, float zoom) in images)
            {
                using (new SKAutoCanvasRestore(canvas))
                {
                    float resizeX = 360f * _transform.Zoom / 256 / zoom;
                    float resizeY = (float)(GeomTransform.Height * 2) * _transform.Zoom / 256 / zoom;
                    canvas.Scale(resizeX, resizeY);
                    canvas.DrawBitmap(bitmap, point.X / (float)resizeX, point.Y / (float)resizeY);
                }
            }
            
        }

        private protected override void Initialize(GeomTransform geo)
        {
            _transform = geo;
            Task.Run(async () => await LoadBitmapAsync(135, 35, 12));
        }
        public static async Task<SKBitmap> LoadBitmapFromUrlAsync(string url)
        {
            // URLから画像をダウンロード
            using HttpClient webClient = new();
            Debug.WriteLine(url + "にリクエストを送信します");
            byte[] network = await webClient.GetByteArrayAsync(url);
            SKBitmap bitmap = SKBitmap.Decode(network);
            
            return bitmap;
        }
        private async Task LoadBitmapAsync(double lon, double lat, int zoom) {
            if (_transform is null) return;
            GetTileLeftTop(lat, lon, zoom, out double left, out double top, out int x, out int y, out int z);
            SKBitmap bitmap = await LoadBitmapFromUrlAsync(GenerateUrl(_source, x, y, z));
            images.Add(
                (
                    bitmap,
                    _transform.Translate(left, top),
                    MathF.Pow(2, z)
                )
            );
        }
        public static string GenerateUrl(string source, int x, int y, int zoom)
        {
            return source.Replace("{x}", x.ToString()).Replace("{y}", y.ToString()).Replace("{z}", zoom.ToString());
        }
        public static void GetXYZTile(double latitude, double longitude, int zoom, out int x, out int y, out int z)
        {
            double n = Math.Pow(2, zoom);
            double lat_rad = latitude * Math.PI / 180.0;

            x = (int)Math.Floor((longitude + 180.0) / 360.0 * n);
            y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(lat_rad) + 1.0 / Math.Cos(lat_rad)) / Math.PI) / 2.0 * n);
            z = zoom;
        }
        public static void GetTileLeftTop(double latitude, double longitude, int zoom, out double left, out double top, out int x, out int y, out int z)
        {
            GetXYZTile(latitude, longitude, zoom, out x, out y, out z);

            double n = Math.Pow(2, z);
            double lon_deg = x / n * 360.0 - 180.0;
            double lat_rad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / n)));
            double lat_deg = lat_rad * 180.0 / Math.PI;

            left = lon_deg;
            top = lat_deg;
        }
    }
}
