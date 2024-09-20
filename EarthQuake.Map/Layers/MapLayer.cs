using System.Reflection;
using SkiaSharp;

namespace EarthQuake.Map.Layers;

public abstract class MapLayer
{
    private bool initialized;
    public static readonly SKTypeface Font = LoadFont();

    private static SKTypeface LoadFont()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EarthQuake.Map.Assets.NotoSansJP-Regular.ttf");
        return SKTypeface.FromStream(stream);
    }
    public abstract void Render(SKCanvas canvas, float scale, SKRect bounds);
    private protected abstract void Initialize();
    public void Update()
    {
        if (initialized) return;
        Initialize();
        initialized = true;
    }
}