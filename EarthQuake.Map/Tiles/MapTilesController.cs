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

    private static void GetXyzTile(double screenX, double screenY, int zoom, out int x, out int y, out int z)
    {
        z = Math.Min(18, Math.Max(0, zoom));
        var n = Math.Pow(2, z);

        x = (int)Math.Floor((screenX + 180.0) / 360.0 * n);
        y = (int)Math.Floor((1.0 - screenY / GeomTransform.Height) / 2.0 * n);
    }

    public static void GetXyzTile(SKPoint screen, int zoom, out TilePoint point)
    {
        GetXyzTile(screen.X, screen.Y, zoom, out var x, out var y, out var z);
        point = new TilePoint(Math.Max(0, x), Math.Max(0, y), z);
    }

    /*private static void GetTileLeftTop(double screenX, double screenY, int zoom, out double left, out double top, out int x, out int y, out int z)
    {
        GetXyzTile(screenX, screenY, zoom, out x, out y, out z);

        var n = Math.Pow(2, z);
        var lonDeg = x / n * 360.0 - 180.0;
        var latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / n)));
        var latDeg = latRad * 180.0 / Math.PI;

        left = lonDeg;
        top = latDeg;
    }*/
    private static void GetTileLeftTop(int x, int y, int z, out double left, out double top)
    {
        var n = Math.Pow(2, z);
        var lonDeg = x / n * 360.0 - 180.0;
        var latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / n)));
        var latDeg = latRad * 180.0 / Math.PI;

        left = lonDeg;
        top = latDeg;
    }
    /*
    /// <summary>
    /// 緯度・経度からタイルを取得します。
    /// </summary>
    /// <param name="lon">経度</param>
    /// <param name="lat">緯度</param>
    /// <param name="zoom">ズームレベル</param>
    /// <param name="tile">出力</param>
    /// <param name="tilePoint">その位置</param>
    /// <returns></returns>
    public bool TryGetTile(double lon, double lat, int zoom, out T? tile, out TilePoint tilePoint)
    {

        tile = default;

        GetTileLeftTop(lon, GeomTransform.TranslateFromLat(lat), zoom, out var left, out var top, out var x, out var y, out var z);
        tilePoint = new TilePoint(x, y, z);
        return GetTile(ref tile, tilePoint, left, top);
    }
    */

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

            if (MapRequestHelper.Any(req => RequestExists(req, point))) return false;
            {
                MapRequestHelper.AddRequest(GenerateRequest(leftTop, point));
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
            return false;
        }
    }

    private protected abstract MapTileRequest GenerateRequest(SKPoint point, TilePoint tilePoint);

    private protected abstract bool RequestExists(MapRequest request, TilePoint tilePoint);

    private static string GenerateUrl(string source, int x, int y, int zoom)
    {
        return source.Replace("{x}", x.ToString()).Replace("{y}", y.ToString()).Replace("{z}", zoom.ToString());
    }

    private protected static string GenerateUrl(string source, TilePoint tilePoint)
    {
        return GenerateUrl(source, tilePoint.X, tilePoint.Y, tilePoint.Z);
    }
}