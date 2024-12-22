using EarthQuake.Core;
using SkiaSharp;
using System.Diagnostics;
using EarthQuake.Map.Tiles.Request;

namespace EarthQuake.Map.Tiles;

public abstract class MapTilesController<T>(string url, int capacity = 256) where T : class
{
    public Action? OnUpdate;
    public const int ImageSize = 256;
    private protected readonly string Url = url;
    private protected readonly LRUCache<TilePoint, T> Tiles = new(capacity);

    /// <summary>
    /// 位置からXYZタイルを取得します。
    /// </summary>
    /// <param name="screenX">画面上のX=経度</param>
    /// <param name="screenY">画面上のY=メルカトル図法で変換された緯度</param>
    /// <param name="zoom">ズームレベル</param>
    /// <returns>タイルの位置</returns>
    private static TilePoint GetXyzTile(double screenX, double screenY, int zoom)
    {
        var z = Math.Min(18, Math.Max(0, zoom));
        var n = Math.Pow(2, z);

        var x = (int)Math.Floor((screenX + 180.0) / 360.0 * n);
        var y = (int)Math.Floor((1.0 - screenY / GeomTransform.Height) / 2.0 * n);
        return new TilePoint(Math.Max(0, x), Math.Max(0, y), z);
    }

    /// <summary>
    /// 位置からXYZタイルを取得します。
    /// </summary>
    /// <param name="screen">メルカトル図法で変換された位置</param>
    /// <param name="zoom">ズームレベル</param>
    /// <param name="point">位置</param>
    public static void GetXyzTile(SKPoint screen, int zoom, out TilePoint point)
    {
        point = GetXyzTile(screen.X, screen.Y, zoom);
    }
    
    /// <summary>
    /// 経度緯度からXYZタイルを取得します。
    /// </summary>
    /// <param name="lon">経度</param>
    /// <param name="lat">緯度</param>
    /// <param name="zoom">ズーム</param>
    /// <param name="point">位置</param>
    public static void GetXyzTileFromLatLon(double lon, double lat, int zoom, out TilePoint point)
    {
        var screen = GeomTransform.Mercator(lat);
        point = GetXyzTile(lon, screen, zoom);
    }
    
    private static void GetTileLeftTop(int x, int y, int z, out double left, out double top)
    {
        var n = Math.Pow(2, z);
        var lonDeg = x / n * 360.0 - 180.0;
        var latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / n)));
        var latDeg = latRad * 180.0 / Math.PI;

        left = lonDeg;
        top = latDeg;
    }

    /// <summary>
    /// タイルの位置(XYZ)からタイルを取得します。
    /// </summary>
    /// <param name="tilePoint">タイルの位置</param>
    /// <param name="tile">出力のタイル</param>
    /// <returns>タイルが存在したかどうか</returns>
    public bool TryGetTile(TilePoint tilePoint, out T? tile)
    {
        tile = default;
        GetTileLeftTop(tilePoint.X, tilePoint.Y, tilePoint.Z, out var left, out var top);
        return GetTile(ref tile, tilePoint, left, top);
    }

    private bool GetTile(ref T? tile, TilePoint point, double left, double top)
    {
        try
        {
            var leftTop = GeomTransform.Translate(left, top);

            if (Tiles.TryGet(point, out var value))
            {
                return (tile = value) is not null;
            }

            var request = GenerateRequest(leftTop, point);
            if (MapRequestHelper.Exists(request))
            {
                return false;
            }
            MapRequestHelper.AddRequest(request);
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
            return false;
        }
    }

    private protected abstract MapTileRequest GenerateRequest(SKPoint point, TilePoint tilePoint);

    private static string GenerateUrl(string source, int x, int y, int zoom)
    {
        return source.Replace("{x}", x.ToString()).Replace("{y}", y.ToString()).Replace("{z}", zoom.ToString());
    }

    private protected static string GenerateUrl(string source, TilePoint tilePoint)
    {
        return GenerateUrl(source, tilePoint.X, tilePoint.Y, tilePoint.Z);
    }
}