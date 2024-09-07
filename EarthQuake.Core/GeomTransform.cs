using SkiaSharp;

namespace EarthQuake.Core
{
    public class GeomTransform
    {
        
        public const int Zoom = 50;
        private static readonly SKPoint Offset = new(135, (float)TranslateFromLat(35));
        public const int Height = 150;
        private const double MercatorLimit = 85.05112877980659;
        public static SKPoint Translate(double lon, double lat)
        {
            var x = (float)(lon - Offset.X) * Zoom;
            var y = -(float)(TranslateFromLat(lat) - Offset.Y) * Zoom;
            return new SKPoint(x, y);
        }
        public static SKPoint TranslateToNonTransform(float x, float y) => new(x / Zoom + Offset.X, Offset.Y - y / Zoom);
        /// <summary>
        /// 緯度から計算された位置に変換します。
        /// </summary>
        /// <param name="latitude">緯度</param>
        /// <returns></returns>
        public static double TranslateFromLat(double latitude) => Mercator(latitude);
        
        /// <summary>
        /// メルカトル図法
        /// </summary>
        /// <param name="latitude">緯度</param>
        /// <returns></returns>
        private static double Mercator(double latitude) => latitude <= -MercatorLimit ? -Height : latitude >= MercatorLimit ? Height : Math.Log(Math.Tan((90 + latitude) * Math.PI / 360)) * Height / Math.PI;
        
        /// <summary>
        /// ミラー図法
        /// </summary>
        /// <param name="latitude">緯度</param>
        /// <returns></returns>
        public static double Mirror(double latitude) => 1.25 * Math.Asinh(Math.Tan(0.8 * latitude * Math.PI / 360)) * Height;
        
        // Web Mercator
        public static double WebMercator(double latitude) => Math.Log(Math.Tan((90 + latitude) * Math.PI / 360)) / Math.PI;
        
        public static SKPoint Translate(SKPoint point) => Translate(point.X, point.Y);
        public static SKPoint Translate(float lon, float lat)
        {
            var x = (lon - Offset.X) * Zoom;
            var y = -((float)TranslateFromLat(lat) - Offset.Y) * Zoom;
            return new SKPoint(x, y);
        }
        public static int RealIndex(int value)
        {
            return value >= 0 ? value : -value - 1;
        }
    }
}
