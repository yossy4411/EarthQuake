﻿using EarthQuake.Map.Layers;
using EarthQuake.Map.Layers.OverLays;
using SkiaSharp;


namespace EarthQuake.Map;

/// <summary>
/// マップビューのコントローラー
/// </summary>
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
                cacheableLayer.OnUpdated += () => { OnUpdated?.Invoke(); };
            }

            _mapLayers = value.Where(x => x is not CacheableLayer);
        }
    }

    public Action? OnUpdated;

    public void Render(SKCanvas canvas, float scale, SKRect bounds)
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