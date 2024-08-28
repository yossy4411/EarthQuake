using EarthQuake.Core;
using SkiaSharp;
using System.Diagnostics;

namespace EarthQuake.Map
{
    /// <summary>
    /// LRU (Least Recently Used) を使用したキャッシュの保存
    /// </summary>
    /// <typeparam name="K">Key</typeparam>
    /// <typeparam name="V">Value</typeparam>
    /// <param name="capacity"></param>
    public class LruCache<K, V>(int capacity) where K : IEquatable<K>
    {
        private readonly Dictionary<K, LinkedListNode<(K key, V value)>> cache = [];
        private readonly LinkedList<(K key, V value)> lruList = [];

        public bool TryGet(K key, out V? value)
        {
            if (cache.TryGetValue(key, out var node))
            {
                // キャッシュヒット: データをリストの先頭に移動
                lruList.Remove(node);
                lruList.AddFirst(node);
                value = node.Value.value;
                return true;
            }
            value = default;
            return false;
        }
        public void Put(K key, V value)
        {
            if (cache.TryGetValue(key, out var node))
            {
                // キャッシュヒット: データを更新し、リストの先頭に移動
                lruList.Remove(node);
                node.Value = (key, value);
                lruList.AddFirst(node);
            }
            else
            {
                if (cache.Count >= capacity)
                {
                    // キャッシュが満杯: 最後の要素（LRU）を削除
                    var lru = lruList.Last;
                    if (lru != null)
                    {
                        cache.Remove(lru.Value.key);
                        lruList.RemoveLast();
                    }
                }
                // 新しいデータを追加
                var newNode = new LinkedListNode<(K key, V value)>((key, value));
                lruList.AddFirst(newNode);
                cache[key] = newNode;
            }
        }
    }

    public abstract class MapTilesController<T>
    {
        public const int ImageSize = 256;
        private protected readonly string Url;
        public GeomTransform? Transform { get; set; }
        private readonly LruCache<TilePoint, T> images = new(100);
        private readonly List<MapTileRequest> requests = [];
        private readonly Task[] tasks = new Task[1];
        protected MapTilesController(string url)
        {
            Url = url;
            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(Handle);
            }
        }
        private async Task Handle()
        {
            HttpClient client = new();
            while (true)
            {
                if (requests.Count <= 0) continue;
                try
                {
                    var req = requests[0];
                    await GetTile(client, 
                        req.Point, 
                        req.TilePoint);
                        
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.StackTrace);
                }
                finally
                {
                    requests.RemoveAt(0);
                }
            }
        }

        private protected abstract Task<T> GetTile(HttpClient client, SKPoint point, TilePoint point1);
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
        /// <summary>
        /// 緯度・経度からタイルを取得します。
        /// </summary>
        /// <param name="lon">経度</param>
        /// <param name="lat">緯度</param>
        /// <param name="zoom">ズームレベル</param>
        /// <param name="tile"></param>
        /// <param name="tilePoint"></param>
        /// <returns></returns>
        public bool TryGetTile(double lon, double lat, int zoom, out T? tile, out TilePoint tilePoint)
        {

            tile = default;
            if (Transform is null)
            {
                tilePoint = TilePoint.Empty;
                return false;
            }
            GetTileLeftTop(lon, GeomTransform.TranslateFromLat(lat), zoom, out var left, out var top, out var x, out var y, out var z);
            tilePoint = new TilePoint(x, y, z);
            return GetTile(ref tile, tilePoint, left, top);
        }
        
        /// <summary>
        /// タイルの位置(XYZ)からタイルを取得します。
        /// </summary>
        /// <param name="tilePoint">タイルの位置</param>
        /// <param name="tile"></param>
        /// <returns></returns>
        public bool TryGetTile(TilePoint tilePoint, out T? tile)
        {
            tile = default;
            if (Transform is null)
            {
                return false;
            }
            GetTileLeftTop(tilePoint.X, tilePoint.Y, tilePoint.Z, out var left, out var top);
            return GetTile(ref tile, tilePoint, left, top);
        }
        private bool GetTile(ref T? tile, TilePoint point, double left, double top)
        {
            try
            {
                var leftTop = Transform!.Translate(left, top);

                if (images.TryGet(point, out var value))
                {
                    return (tile = value) is not null;
                }

                if (requests.Any(x => x.TilePoint == point)) return false;
                {
                    requests.RemoveAll(x => x.TilePoint.Z != point.Z);
                    requests.Add(new MapTileRequest(leftTop, point));
                }
                return false;
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
                return false;
            }
        }
        
        private protected static string GenerateUrl(string source, int x, int y, int zoom)
        {
            return source.Replace("{x}", x.ToString()).Replace("{y}", y.ToString()).Replace("{z}", zoom.ToString());
        }
        
        private record MapTileRequest(SKPoint Point, TilePoint TilePoint);
        public readonly record struct TilePoint(int X, int Y, int Z)
        {
            public static readonly TilePoint Empty = new();
            public TilePoint Add(int x, int y) => new(X + x, Y + y, Z);
            public static TilePoint operator +(TilePoint point, (int x, int y)point1) => point.Add(point1.x, point1.y);
        }
        
    }
    
    public class RasterTilesController(string url) : MapTilesController<RasterTilesController.RasterTile>(url)
    { 
        public record RasterTile(SKPoint LeftTop, float Zoom, SKBitmap? Image);

        private static async Task<SKBitmap?> LoadBitmapFromUrlAsync(HttpClient webClient, string url)
        {
            var response = await webClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) return null;
            var network = await response.Content.ReadAsByteArrayAsync();
            return SKBitmap.Decode(network);

        }
        private protected override async Task<RasterTile> GetTile(HttpClient client, SKPoint point, TilePoint point1)
        {
            var bitmap = await LoadBitmapFromUrlAsync(client, GenerateUrl(Url, point1.X, point1.Y, point1.Z));
            return new RasterTile(point, MathF.Pow(2, point1.Z), bitmap);
        }
    }
    
}
