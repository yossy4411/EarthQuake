using EarthQuake.Map.Layers;
using EarthQuake.Map.Layers.OverLays;
using SkiaSharp;


namespace EarthQuake.Map;

public class MapViewController
{
    private readonly IEnumerable<MapLayer> _mapLayers = [];
    private readonly IEnumerable<CacheableLayer> cacheableLayers = [];
    private SKPicture? cached;
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
                cacheableLayer.OnUpdated += () =>
                {
                    OnUpdated?.Invoke();
                };
            }
            _mapLayers = value.Where(x => x is not CacheableLayer);
        }
    }

    public Action? OnUpdated;

    public void RenderBase(SKCanvas canvas, float scale, SKRect bounds)
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
        foreach (var layer in _mapLayers)
        {
            if (layer is ForeGroundLayer) continue;
            layer.Render(canvas, scale, bounds);
        }
    }
    public void RenderForeGround(SKCanvas canvas, float scale, SKRect bounds, object? param = null)
    {
        foreach (var layer in _mapLayers)
        {
            if (layer is not ForeGroundLayer fore) continue;
            if (fore is Hypo3DViewLayer hypo)
                hypo.Render(canvas, scale, param as SKRect? ?? SKRect.Empty);
            else
                fore.Render(canvas, scale, bounds);

        }
    }
}