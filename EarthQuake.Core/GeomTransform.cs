using SkiaSharp;

namespace EarthQuake.Core
{
    public class GeomTransform
    {
        
        public static int Zoom => 50;
        private readonly SKPoint offset = new(135, (float)TranslateFromLat(35));
        public const int Height = 150;
        private const double MercatorLimit = 85.05112877980659;
        public SKPoint Translate(double lon, double lat)
        {
            var x = (float)(lon - offset.X) * Zoom;
            var y = -(float)(TranslateFromLat(lat) - offset.Y) * Zoom;
            return new SKPoint(x, y);
        }
        public SKPoint TranslateToNonTransform(float x, float y) => new(x / Zoom + offset.X, offset.Y - y / Zoom);
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
        
        public SKPoint Translate(SKPoint point) => Translate(point.X, point.Y);
        public SKPoint Translate(float lon, float lat)
        {
            var x = (lon - offset.X) * Zoom;
            var y = -((float)TranslateFromLat(lat) - offset.Y) * Zoom;
            return new SKPoint(x, y);
        }
        public static int RealIndex(int value)
        {
            return value >= 0 ? value : -value - 1;
        }
    }
}
