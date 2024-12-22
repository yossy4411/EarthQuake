using EarthQuake.Map.Layers;
using EarthQuake.Map.Layers.OverLays;
using EarthQuake.Map.Tiles.Vector;
using SkiaSharp;
using VectorTiles.Styles;
using VectorTiles.Values;


namespace EarthQuake.Map;

/// <summary>
/// マップビューのコントローラー
/// </summary>
public class MapViewController
{
    private readonly IEnumerable<MapLayer> _mapLayers = [];
    private readonly IEnumerable<CacheableLayer> cacheableLayers = [];
    private SKPicture? cached;

    /// <summary>
    /// キャッシュを使用するかどうか
    /// </summary>
    public bool UseCache { get; set; } = true;

    /// <summary>
    /// キャッシュされたレイヤーのキャッシュを更新する
    /// </summary>
    public void UpdateCache()
    {
        foreach (var cacheableLayer in cacheableLayers)
        {
            cacheableLayer.IsUpdated = true;
        }
    }

    /// <summary>
    /// 描画レイヤー
    /// </summary>
    public MapLayer[] MapLayers
    {
        init
        {
            foreach (var item in value)
            {
                item.Update();
            }

            cacheableLayers = value.OfType<CacheableLayer>();
            foreach (var cacheableLayer in cacheableLayers)
            {
                cacheableLayer.OnUpdated += () => { OnUpdated?.Invoke(); };
            }

            _mapLayers = value.Where(x => x is not CacheableLayer);
        }
    }

    /// <summary>
    /// 更新されたときのイベント
    /// </summary>
    public Action? OnUpdated;

    // ズームの値のキャッシュ
    private readonly Dictionary<string, IConstValue?> _zoomCache = new();

    /// <summary>
    /// 動的な背景色
    /// </summary>
    public VectorBackgroundStyleLayer? Background { get; init; }

    /// <summary>
    /// キャンバスをクリアする
    /// </summary>
    public void Clear(SKCanvas canvas, float zoom)
    {
        _zoomCache["$zoom"] = new ConstFloatValue(MathF.Log2(zoom) + 5);
        canvas.Clear(Background?.BackgroundColor is not null
            ? Background.BackgroundColor.GetValue(_zoomCache)!.ToColor().ToSKColor()
            : SKColors.Silver);
    }

    /// <summary>
    /// 描画する
    /// </summary>
    /// <param name="canvas">キャンバス</param>
    /// <param name="scale">ズームレベル</param>
    /// <param name="bounds">表示範囲</param>
    public void Render(SKCanvas canvas, float scale, SKRect bounds)
    {
        if (UseCache)
        {
            if (cacheableLayers.Any(x => x.IsUpdated || x.ShouldReload(scale, bounds)))
            {
                cached?.Dispose();
                cached = null;
                using var recorder = new SKPictureRecorder();
                using var c = recorder.BeginRecording(bounds);
                foreach (var cacheableLayer in cacheableLayers)
                {
                    cacheableLayer.IsUpdated = false;
                    cacheableLayer.Render(c, scale, bounds);
                }

                cached = recorder.EndRecording();
            }

            if (cached is not null)
            {
                canvas.DrawPicture(cached);
            }
        }
        else
        {
            foreach (var cacheableLayer in cacheableLayers)
            {
                cacheableLayer.Render(canvas, scale, bounds);
            }
        }

        // Background
        foreach (var layer in _mapLayers)
        {
            if (layer is ForeGroundLayer) continue;
            layer.Render(canvas, scale, bounds);
        }

        // Foreground
        using (new SKAutoCanvasRestore(canvas))
        {
            canvas.Scale(1 / scale);
            foreach (var layer in _mapLayers)
            {
                if (layer is not ForeGroundLayer) continue;
                layer.Render(canvas, scale, bounds);
            }
        }
    }
}