using System.Collections.Concurrent;
using EarthQuake.Core;
using SkiaSharp;
using System.Diagnostics;
using EarthQuake.Map.Tiles.Request;

namespace EarthQuake.Map.Tiles
{
    /// <summary>
    /// LRU (Least Recently Used) を使用したキャッシュの保存
    /// </summary>
    /// <typeparam name="T1">Key</typeparam>
    /// <typeparam name="T2">Value</typeparam>
    /// <param name="capacity">最大保存量</param>
    internal class LRUCache<T1, T2>(int capacity) where T1 : IEquatable<T1>
    {
        private readonly Dictionary<T1, LinkedListNode<(T1 key, T2 value)>> _cache = [];
        private readonly LinkedList<(T1 key, T2 value)> _lruList = [];

        public bool TryGet(T1 key, out T2? value)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                // キャッシュヒット: データをリストの先頭に移動
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                value = node.Value.value;
                return true;
            }
            value = default;
            return false;
        }
        public void Put(T1 key, T2 value)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                // キャッシュヒット: データを更新し、リストの先頭に移動
                _lruList.Remove(node);
                node.Value = (key, value);
                _lruList.AddFirst(node);
            }
            else
            {
                if (_cache.Count >= capacity)
                {
                    RemoveCache();
                }
                // 新しいデータを追加
                var newNode = new LinkedListNode<(T1 key, T2 value)>((key, value));
                _lruList.AddFirst(newNode);
                _cache[key] = newNode;
            }
        }

        private void RemoveCache()
        {
            // キャッシュが満杯: 最後の要素（LRU）を削除
            var lru = _lruList.Last;
            if (lru is null) return;
            _cache.Remove(lru.Value.key);
            _lruList.RemoveLast();
            if (lru.Value.value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public abstract class MapTilesController<T>(string url) where T : class
    {
        public const int ImageSize = 256;
        private protected readonly string Url = url;
        private protected readonly LRUCache<TilePoint, T> Tiles = new(256);
        
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
        private static void GetTileLeftTop(double screenX, double screenY, int zoom, out double left, out double top, out int x, out int y, out int z)
        {
            GetXyzTile(screenX, screenY, zoom, out x, out y, out z);

            var n = Math.Pow(2, z);
            var lonDeg = x / n * 360.0 - 180.0;
            var latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / n)));
            var latDeg = latRad * 180.0 / Math.PI;

            left = lonDeg;
            top = latDeg;
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
            } catch (Exception ex)
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
    
}
