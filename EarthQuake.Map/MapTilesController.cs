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
    public class LRUCache<K, V>(int capacity) where K : IEquatable<K>
    {
        private readonly int capacity = capacity;
        private readonly Dictionary<K, LinkedListNode<(K key, V value)>> cache = [];
        private readonly LinkedList<(K key, V value)> lruList = new();

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
    internal class MapTilesController
    {
        private class MapTileReciever { 
        }
        private readonly string _url;
        public GeomTransform? Transform { get; set; }
        private readonly LRUCache<TilePoint, MapTile> images = new(100);
        private readonly List<MapTileRequest> requests = [];
        private readonly Task[] tasks = new Task[1];
        internal MapTilesController(string url)
        {
            _url = url;
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(Handle);
            }
        }
        private async Task Handle()
        {
            HttpClient client = new();
            while (true)
            {
                if (requests.Count > 0)
                {
                    try
                    {
                        var req = requests[0];
                        await GetBitmapAsync(client, 
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
        }
        private async Task GetBitmapAsync(HttpClient client, SKPoint point, TilePoint point1)
        {
            SKBitmap? bitmap = await LoadBitmapFromUrlAsync(client, GenerateUrl(_url, point1.X, point1.Y, point1.Z));
            MapTile tile = new(point, MathF.Pow(2, point1.Z), bitmap, point1);
            images.Put(point1, tile);
        }
        private static async Task<SKBitmap?> LoadBitmapFromUrlAsync(HttpClient webClient, string url)
        {
            var response = await webClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                byte[] network = await response.Content.ReadAsByteArrayAsync();
                return SKBitmap.Decode(network)/*.Resize(new SKImageInfo(512, 512), SKFilterQuality.Medium)*/;
            }
            else
            {
                return null;
            }

        }
        private static void GetXYZTile(double screenX, double screenY, int zoom, out int x, out int y, out int z)
        {
            z = Math.Min(18, Math.Max(0, zoom));
            double n = Math.Pow(2, z);

            x = (int)Math.Floor((screenX + 180.0) / 360.0 * n);
            y = (int)Math.Floor((1.0 - screenY / GeomTransform.Height) / 2.0 * n);
        }
        public static void GetXYZTile(SKPoint screen, int zoom, out TilePoint point)
        {
            GetXYZTile(screen.X, screen.Y, zoom, out int x, out int y, out int z);
            point = new(Math.Max(0, x), Math.Max(0, y), z);
        }
        private static void GetTileLeftTop(double screenX, double screenY, int zoom, out double left, out double top, out int x, out int y, out int z)
        {
            GetXYZTile(screenX, screenY, zoom, out x, out y, out z);

            double n = Math.Pow(2, z);
            double lon_deg = x / n * 360.0 - 180.0;
            double lat_rad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / n)));
            double lat_deg = lat_rad * 180.0 / Math.PI;

            left = lon_deg;
            top = lat_deg;
        }
        private static void GetTileLeftTop(int x, int y, int z, out double left, out double top)
        {
            double n = Math.Pow(2, z);
            double lon_deg = x / n * 360.0 - 180.0;
            double lat_rad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / n)));
            double lat_deg = lat_rad * 180.0 / Math.PI;

            left = lon_deg;
            top = lat_deg;
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
        public bool TryGetTile(double lon, double lat, int zoom, out MapTile? tile, out TilePoint tilePoint)
        {

            tile = null;
            if (Transform is null)
            {
                tilePoint = TilePoint.Empty;
                return false;
            }
            GetTileLeftTop(lon, GeomTransform.Mercator(lat), zoom, out double left, out double top, out int x, out int y, out int z);
            tilePoint = new(x, y, z);
            return GetTile(ref tile, tilePoint, left, top);
        }

        /// <summary>
        /// 変換された位置からタイルを取得します。
        /// </summary>
        /// <param name="translated">TranslateToNonTransformされた位置</param>
        /// <param name="zoom">ズームレベル</param>
        /// <param name="tile"></param>
        /// <param name="tilePoint"></param>
        /// <returns></returns>
        public bool TryGetTile(SKPoint translated, int zoom, out MapTile? tile, out TilePoint tilePoint)
        {

            tile = null;
            if (Transform is null)
            {
                tilePoint = TilePoint.Empty;
                return false;
            }
            GetTileLeftTop(translated.X, translated.Y, zoom, out double left, out double top, out int x, out int y, out int z);
            tilePoint = new(x, y, z);
            return GetTile(ref tile, tilePoint, left, top);
        }
        /// <summary>
        /// タイルの位置(XYZ)からタイルを取得します。
        /// </summary>
        /// <param name="tilePoint">タイルの位置</param>
        /// <param name="tile"></param>
        /// <returns></returns>
        public bool TryGetTile(TilePoint tilePoint, out MapTile? tile)
        {
            tile = null;
            if (Transform is null)
            {
                return false;
            }
            GetTileLeftTop(tilePoint.X, tilePoint.Y, tilePoint.Z, out double left, out double top);
            return GetTile(ref tile, tilePoint, left, top);
        }
        private bool GetTile(ref MapTile? tile, TilePoint point, double left, double top)
        {
            SKPoint leftTop = Transform!.Translate(left, top);

            if (images.TryGet(point, out MapTile? value))
            {
                tile = value;
                return true;
            }
            if (!requests.Any(x => x.TilePoint == point))
            {
                requests.RemoveAll(x => x.TilePoint.Z != point.Z);
                requests.Add(new(leftTop, point));

            }
            return false;
        }
        private static string GenerateUrl(string source, int x, int y, int zoom)
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
        public record MapTile(SKPoint LeftTop, float Zoom,  SKBitmap? Image, TilePoint Point);
    }
    
}
