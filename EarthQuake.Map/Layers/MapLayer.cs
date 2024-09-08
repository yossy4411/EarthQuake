using Avalonia.Platform;
using EarthQuake.Core;
using SkiaSharp;

namespace EarthQuake.Map.Layers;

public abstract class MapLayer
{
    private bool initialized;
    public static readonly SKTypeface Font = SKTypeface.FromStream(AssetLoader.Open(new Uri("avares://EarthQuake/Assets/Fonts/NotoSansJP-Medium.ttf")));
    internal abstract void Render(SKCanvas canvas, float scale, SKRect bounds);
    private protected abstract void Initialize(GeomTransform geo);
    public void Update(GeomTransform geo)
    {
        if (initialized) return;
        Initialize(geo);
        initialized = true;
    }
}