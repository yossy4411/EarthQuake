using SkiaSharp;

namespace EarthQuake.Map.Layers;

public interface ICacheableLayer
{
    public bool IsUpdated { get; set; }
    public void Render(SKCanvas canvas, float scale, SKRect bounds);
    public bool IsReloadRequired(float scale, SKRect bounds);
}