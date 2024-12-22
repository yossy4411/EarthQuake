using SkiaSharp;

namespace EarthQuake.Map.Layers;

/// <summary>
/// キャッシュ可能なレイヤー
/// </summary>
public abstract class CacheableLayer : MapLayer
{
    /// <summary>
    /// キャッシュを更新するべきか
    /// </summary>
    /// <param name="zoom">ズームレベル</param>
    /// <param name="bounds">表示範囲</param>
    /// <returns></returns>
    public abstract bool ShouldReload(float zoom, SKRect bounds);


    /// <summary>
    /// 更新されたときのイベント
    /// </summary>
    public Action? OnUpdated { get; set; }

    /// <summary>
    /// 更新されたかどうか
    /// </summary>
    public bool IsUpdated { get; set; }

    /// <summary>
    /// 更新されたときの処理
    /// </summary>
    private protected void HandleUpdated()
    {
        OnUpdated?.Invoke();
        IsUpdated = true;
    }
}