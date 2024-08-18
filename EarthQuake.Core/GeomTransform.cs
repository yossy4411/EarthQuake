using SkiaSharp;
using static EarthQuake.Core.TopoJson.MapData;

namespace EarthQuake.Core
{
    public class GeomTransform
    {
        
        public int Zoom { get; set; } = 50;
        internal SKPoint Offset { get; set; } = new(135, (float)TranslateFromLat(35));
        public const int Height = 150;
        private const double mercatorLimit = 85.05112877980659;
        public PolygonType[]? GeometryType { get; set; }
        public SKPoint Translate(double lon, double lat)
        {
            var x = (float)(lon - Offset.X) * Zoom;
            var y = -(float)(TranslateFromLat(lat) - Offset.Y) * Zoom;
            return new(x, y);
        }
        public SKPoint TranslateToNonTransform(float x, float y) => new(x / Zoom + Offset.X, Offset.Y - y / Zoom);
        /// <summary>
        /// 緯度から変換します。
        /// </summary>
        /// <param name="latitude">緯度</param>
        /// <returns></returns>
        public static double TranslateFromLat(double latitude) => Mercator(latitude);
        /// <summary>
        /// メルカトル図法
        /// </summary>
        /// <param name="latitude">緯度</param>
        /// <returns></returns>
        public static double Mercator(double latitude) => latitude <= -mercatorLimit ? -Height : latitude >= mercatorLimit ? Height : Math.Log(Math.Tan((90 + latitude) * Math.PI / 360)) * Height / Math.PI;
        /// <summary>
        /// ミラー図法
        /// </summary>
        /// <param name="latitude">緯度</param>
        /// <returns></returns>
        public static double Mirror(double latitude) => 1.25 * Math.Asinh(Math.Tan(0.8 * latitude * Math.PI / 360)) * Height;
        public SKPoint Translate(SKPoint point) => Translate(point.X, point.Y);
        public SKPoint Translate(float lon, float lat)
        {
            var x = (lon - Offset.X) * Zoom;
            var y = -((float)TranslateFromLat(lat) - Offset.Y) * Zoom;
            return new(x, y);
        }
    }
}
