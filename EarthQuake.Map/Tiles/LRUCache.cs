namespace EarthQuake.Map.Tiles;

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

    /// <summary>
    /// キャッシュを削除します。
    /// </summary>
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