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

    public Action? OnUpdated;
    private readonly Dictionary<string, IConstValue?> _zoomCache = new(); 
    public VectorBackgroundStyleLayer? Background { get; init; }
    public void Clear(SKCanvas canvas, float zoom)
    {
        _zoomCache["$zoom"] = new ConstFloatValue(MathF.Log2(zoom) + 5);
        canvas.Clear(Background?.BackgroundColor is not null
            ? Background.BackgroundColor.GetValue(_zoomCache)!.ToColor().ToSKColor()
            : SKColors.Silver);
    }

    public void Render(SKCanvas canvas, float scale, SKRect bounds)
    {
        if (UseCache)
        {
            if (cacheableLayers.Any(x => x.IsUpdated || x.IsReloadRequired(scale, bounds)))
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