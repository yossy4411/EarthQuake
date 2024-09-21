using SkiaSharp;

namespace EarthQuake.Map.Layers;

public abstract class CacheableLayer : MapLayer
{
    public abstract bool IsReloadRequired(float zoom, SKRect bounds);
    public Action? OnUpdated { get; set; }

    public bool IsUpdated { get;  set; }
    
    private protected void HandleUpdated()
    {
        OnUpdated?.Invoke();
        IsUpdated = true;
    }
}
