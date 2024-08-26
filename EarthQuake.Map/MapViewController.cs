using EarthQuake.Core;
using EarthQuake.Map.Layers;
using EarthQuake.Map.Layers.OverLays;
using SkiaSharp;


namespace EarthQuake.Map;

public class MapViewController(GeomTransform geo)
{
    public GeomTransform Geo { get; init; } = geo;
    private readonly MapLayer[] _mapLayers = [];
    public MapLayer[] MapLayers
    {
        get => _mapLayers;
        init
        {
            _mapLayers = value; 
            foreach (var item in _mapLayers)
            {
                item.Update(Geo);
            }
        }
    }

    public void RenderBase(SKCanvas canvas, float scale, SKRect bounds)
    {
        foreach (var layer in MapLayers)
        {
            if (layer is ForeGroundLayer) continue;
            layer.Render(canvas, scale, bounds);
        }

    }
    public void RenderForeGround(SKCanvas canvas, float scale, SKRect bounds, object? param = null)
    {
        foreach (var layer in MapLayers)
        {
            if (layer is not ForeGroundLayer fore) continue;
            if (fore is Hypo3DViewLayer hypo)
                hypo.Render(canvas, scale, param as SKRect? ?? SKRect.Empty);
            else
                fore.Render(canvas, scale, bounds);

        }
            
    }
}