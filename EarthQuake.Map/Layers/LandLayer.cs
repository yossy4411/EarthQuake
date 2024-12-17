﻿using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Core.TopoJson;
using EarthQuake.Map.Colors;
using SkiaSharp;
using EarthQuake.Map.Tiles.File;

namespace EarthQuake.Map.Layers;

/// <summary>
/// 地形の塗りつぶしを行うレイヤー
/// </summary>
public class LandLayer : CacheableLayer
{
    public bool Draw { get; set; } = true;
    private SKColor[]? colors;
    private readonly string[]? names;

    private FileTilesController? fileTilesController;
    private int previousScale = -1;
    private readonly PolygonsSet? _polygons;
    private readonly string _layerName;
    private readonly bool _autoFill;

    /// <summary>
    /// 地形の塗りつぶしを行うレイヤー
    /// </summary>
    /// <param name="polygons">ポリゴンデータ</param>
    /// <param name="layerName">データから読み込むレイヤー名</param>
    /// <param name="autoFill">自動塗りつぶし</param>
    public LandLayer(PolygonsSet? polygons, string layerName, bool autoFill = false)
    {
        _polygons = polygons;
        _layerName = layerName;
        names = polygons?.Filling[layerName].Names;
        _autoFill = autoFill;
        if (autoFill)
        {
            colors = Enumerable.Repeat(SKColors.DarkGreen, names?.Length ?? 0).ToArray();
        }
    }

    private protected override void Initialize()
    {
        fileTilesController = _polygons is null
            ? null
            : new FileTilesController(_polygons, _layerName)
            {
                OnUpdate = HandleUpdated
            };
    }

    public void SetInfo(PQuakeData quakeData)
    {
        fileTilesController?.ClearCaches();
        if (names is null || quakeData.Points is null) return;
        colors = new SKColor[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            var a = quakeData.Points.FirstOrDefault(x => x.Addr.StartsWith(name));
            if (a is not null) colors[i] = a.Scale.GetKiwi3Color();
            else if (_autoFill) colors[i] = SKColors.DarkGreen;
            else colors[i] = SKColors.Empty;
        }

        previousScale = -1;
        HandleUpdated();
    }

    public void Reset()
    {
        colors = null;
    }

    public static int GetIndex(float scale)
        => Math.Max(0, Math.Min((int)(-Math.Log(scale * 2, 3) + 3.3), 5));

    public override void Render(SKCanvas canvas, float scale, SKRect bounds)
    {
        // 表示テスト
        var zoom = GetIndex(scale);
        if (!Draw) return;
        using SKPaint paint = new();
        paint.IsAntialias = true;
        paint.Style = SKPaintStyle.Fill;
        if (fileTilesController is null) return;
        if (colors is null) return;
        for (var i = 0; i < colors.Length; i++)
        {
            paint.Color = colors[i];
            if (paint.Color == SKColors.Empty) continue;
            var tile = fileTilesController.TryGetTile(zoom, i);
            if (tile is not null) canvas.DrawVertices(tile, SKBlendMode.Src, paint);
        }
    }

    public override bool IsReloadRequired(float zoom, SKRect bounds)
    {
        if (previousScale == GetIndex(zoom)) return false;
        previousScale = GetIndex(zoom);
        return true;
    }
}