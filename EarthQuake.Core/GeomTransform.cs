using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EarthQuake.Core.TopoJson.MapData;

namespace EarthQuake.Core
{
    public class GeomTransform
    {
        
        public int Zoom { get; set; } = 50;
        internal SKPoint Offset { get; set; } = new(135, (float)Mercator(35));
        public const int Height = 150;
        private const double mercatorLimit = 85.05112877980659;
        public PolygonType[]? GeometryType { get; set; }
        public SKPoint Translate(double lon, double lat)
        {
            float x = (float)(lon - Offset.X) * Zoom;
            float y = -(float)(Mercator(lat) - Offset.Y) * Zoom;
            return new(x, y);
        }
        public SKPoint TranslateToNonTransform(float x, float y) => new(x / Zoom + Offset.X, Offset.Y - y / Zoom);
        public static double Mercator(double latitude) => latitude <= -mercatorLimit ? -Height : latitude >= mercatorLimit ? Height : Math.Log(Math.Tan((90 + latitude) * Math.PI / 360)) * Height / Math.PI;
        public SKPoint Translate(SKPoint point) => Translate(point.X, point.Y);
        public SKPoint Translate(float lon, float lat)
        {
            float x = (lon - Offset.X) * Zoom;
            float y = -((float)Mercator(lat) - Offset.Y) * Zoom;
            return new(x, y);
        }
    }
}
