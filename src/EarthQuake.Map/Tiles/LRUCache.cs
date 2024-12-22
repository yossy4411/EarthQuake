using System.Collections.Concurrent;


namespace EarthQuake.Map.Tiles;
/// <summary>
/// LRU (Least Recently Used) を使用したキャッシュの保存
/// </summary>
/// <typeparam name="T1">Key</typeparam>
/// <typeparam name="T2">Value</typeparam>
/// <param name="capacity">最大保存量</param>

internal class LRUCache<T1, T2>(int capacity) where T1 : IEquatable<T1>
{
    private readonly ConcurrentDictionary<T1, LinkedListNode<(T1 key, T2 value)>> _cache = new();
    private readonly LinkedList<(T1 key, T2 value)> _lruList = [];
    private readonly SemaphoreSlim _semaphore = new(1, 1); // 排他制御用

    public bool TryGet(T1 key, out T2? value)
    {
        if (_cache.TryGetValue(key, out var node))
        {
            // リスト操作は排他制御が必要
            MoveToFirst(node);
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
            // キャッシュヒット: 値を更新し、リストの先頭に移動
            UpdateNode(node, value);
        }
        else
        {
            // キャッシュに新しいデータを追加
            AddNewNode(key, value);
        }
    }

    private void UpdateNode(LinkedListNode<(T1 key, T2 value)> node, T2 value)
    {
        node.Value = (node.Value.key, value);
        MoveToFirst(node);
    }

    private void AddNewNode(T1 key, T2 value)
    {
        _semaphore.Wait(); // 排他制御
        try
        {
            if (_cache.Count >= capacity)
            {
                RemoveLeastRecentlyUsed();
            }

            var newNode = new LinkedListNode<(T1 key, T2 value)>((key, value));
            _lruList.AddFirst(newNode);
            _cache[key] = newNode;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void RemoveLeastRecentlyUsed()
    {
        var lru = _lruList.Last;
        if (lru == null) return;
        _cache.TryRemove(lru.Value.key, out _);
        _lruList.RemoveLast();
        if (lru.Value.value is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void MoveToFirst(LinkedListNode<(T1 key, T2 value)> node)
    {
        _semaphore.Wait(); // 排他制御
        try
        {
            _lruList.Remove(node);
            _lruList.AddFirst(node);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
