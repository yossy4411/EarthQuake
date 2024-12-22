using SkiaSharp;

namespace EarthQuake.Core;

/// <summary>
/// 緯度経度と画面座標の変換
/// </summary>
public static class GeomTransform
{
    public const int Zoom = 50;
    private static readonly SKPoint Offset = new(135, (float)TranslateFromLat(35));
    public const int Height = 150;
    private const double MercatorLimit = 85.05112877980659;

    /// <summary>
    /// 緯度経度の点を計算された位置に変換します。
    /// </summary>
    /// <param name="point">点(X:経度, Y:緯度)</param>
    /// <returns>画面上の座標</returns>
    public static SKPoint Translate(SKPoint point) => Translate(point.X, point.Y);


    public static SKPoint Translate(float lon, float lat)
    {
        var x = (lon - Offset.X) * Zoom;
        var y = -((float)TranslateFromLat(lat) - Offset.Y) * Zoom;
        return new SKPoint(x, y);
    }

    /// <summary>
    /// 経度緯度から計算された位置に変換します。
    /// </summary>
    /// <param name="lon">経度</param>
    /// <param name="lat">緯度</param>
    /// <returns>画面上の座標</returns>
    public static SKPoint Translate(double lon, double lat)
    {
        var x = (float)(lon - Offset.X) * Zoom;
        var y = -(float)(TranslateFromLat(lat) - Offset.Y) * Zoom;
        return new SKPoint(x, y);
    }

    /// <summary>
    /// 画面上の座標からオフセットを戻します。(
    /// </summary>
    /// <param name="x">画面上X</param>
    /// <param name="y">画面上Y</param>
    /// <returns>オフセットのない画面座標</returns>
    public static SKPoint TranslateToNonTransform(float x, float y) => new(x / Zoom + Offset.X, Offset.Y - y / Zoom);

    /// <summary>
    /// 緯度から計算された位置に変換します。
    /// </summary>
    /// <param name="latitude">緯度</param>
    /// <returns>画面上Y</returns>
    private static double TranslateFromLat(double latitude) => Mercator(latitude);

    /// <summary>
    /// メルカトル図法
    /// </summary>
    /// <param name="latitude">緯度</param>
    /// <returns>画面上Y</returns>
    public static double Mercator(double latitude) => latitude <= -MercatorLimit ? -Height :
        latitude >= MercatorLimit ? Height : Math.Log(Math.Tan((90 + latitude) * Math.PI / 360)) * Height / Math.PI;

    /// <summary>
    /// ミラー図法
    /// </summary>
    /// <param name="latitude">緯度</param>
    /// <returns>画面上Y</returns>
    public static double Mirror(double latitude) =>
        1.25 * Math.Asinh(Math.Tan(0.8 * latitude * Math.PI / 360)) * Height;

    public static int RealIndex(int value)
    {
        return value >= 0 ? value : -value - 1;
    }
}