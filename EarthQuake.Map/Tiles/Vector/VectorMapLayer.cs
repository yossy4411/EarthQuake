using SkiaSharp;

namespace EarthQuake.Map.Tiles.Vector;

/// <summary>
/// ベクトルタイルのレイヤー
/// </summary>
/// <param name="source">ソース</param>
/// <param name="filter">フィルタ</param>
public abstract class VectorTileMapLayer(
    string? source = null,
    VectorMapFilter? filter = null) : IDisposable
{
    public string? Source { get; } = source;
    public int MinZoom { get; init; } = 0;
    public int MaxZoom { get; init; } = 22;
    public string? Id { get; init; }

    public bool IsVisible(Dictionary<string, object> values)
    {
        return filter?.Invoke(values) ?? true;
    }

    public abstract VectorTileFeature? CreateFeature(IEnumerable<Mapbox.Vector.Tile.VectorTileFeature> features,
        TilePoint point, float zoom);

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 線形補間する
    /// </summary>
    /// <param name="lineWidth">点の場所</param>
    /// <param name="zoom">ズームレベル</param>
    /// <returns>係数</returns>
    private protected static float Linear(List<(float, float)> lineWidth, float zoom)
    {
        // 1点のみの場合
        if (lineWidth.Count == 1) return lineWidth[0].Item2;
        // 範囲外の場合
        if (zoom < lineWidth[0].Item1) return lineWidth[0].Item2;
        if (zoom >= lineWidth[^1].Item1) return lineWidth[^1].Item2;

        // 2点以上での線形補間
        var (prev, next) = lineWidth.Zip(lineWidth.Skip(1)).First(x => x.First.Item1 <= zoom && zoom < x.Second.Item1);
        var rate = (zoom - prev.Item1) / (next.Item1 - prev.Item1);
        return prev.Item2 + (next.Item2 - prev.Item2) * rate;
    }
}

/// <summary>
/// フィルタ
/// </summary>
public delegate bool VectorMapFilter(Dictionary<string, object> values);

/// <summary>
/// 背景を描画するためのレイヤー
/// </summary>
/// <param name="backgroundColor">背景色</param>
public class VectorBackgroundLayer(SKColor backgroundColor)
    : VectorTileMapLayer
{
    public SKColor BackgroundColor { get; init; } = backgroundColor;

    public override VectorTileFeature? CreateFeature(IEnumerable<Mapbox.Vector.Tile.VectorTileFeature> features,
        TilePoint point, float zoom)
    {
        return null;
    }
}

public class VectorFillLayer(string source, SKColor fillColor, VectorMapFilter? filter = null)
    : VectorTileMapLayer(source, filter)
{
    public SKColor FillColor { get; init; } = fillColor;

    public override VectorTileFeature CreateFeature(IEnumerable<Mapbox.Vector.Tile.VectorTileFeature> features,
        TilePoint point, float zoom)
    {
        return new VectorFillFeature(features, point) { Layer = this };
    }
}

/// <summary>
/// 線を描画するためのレイヤー
/// </summary>
/// <param name="source">ソース</param>
/// <param name="lineColor">線の色</param>
/// <param name="lineWidth">線の太さ</param>
/// <param name="filter">フィルタ</param>
public class VectorLineLayer(
    string source,
    SKColor lineColor,
    List<(float, float)> lineWidth,
    VectorMapFilter? filter = null)
    : VectorTileMapLayer(source, filter)
{
    public SKColor LineColor { get; } = lineColor;
    public SKStrokeCap StrokeCap { get; init; } = SKStrokeCap.Butt;
    public SKStrokeJoin StrokeJoin { get; init; } = SKStrokeJoin.Miter;
    public SKPathEffect? PathEffect { get; init; }

    public override VectorTileFeature CreateFeature(IEnumerable<Mapbox.Vector.Tile.VectorTileFeature> features,
        TilePoint point, float zoom)
    {
        return new VectorLineFeature(features, point) { Layer = this };
    }

    public float GetWidth(float zoom) => Linear(lineWidth, zoom);
}

/// <summary>
/// 記号とかテキストを描画するためのレイヤー
/// </summary>
/// <param name="source">ソース</param>
/// <param name="textColor">テキストの色</param>
/// <param name="typeface">フォント</param>
/// <param name="fontSize">テキストのサイズ</param>
/// <param name="fieldKey">テキストを読み込む場所</param>
/// <param name="filter">フィルタ</param>
public class VectorSymbolLayer(
    string source,
    SKColor textColor,
    SKTypeface typeface,
    List<(float, float)> fontSize,
    string? fieldKey = "name",
    VectorMapFilter? filter = null)
    : VectorTileMapLayer(source, filter)
{
    public SKColor TextColor { get; } = textColor;

    public override VectorTileFeature CreateFeature(IEnumerable<Mapbox.Vector.Tile.VectorTileFeature> features,
        TilePoint point, float zoom)
    {
        var font = typeface.ToFont(Linear(fontSize, point.Z) / zoom * 30);
        return new VectorSymbolFeature(features, point, font, fieldKey) { Layer = this };
    }
}

public class EmptyVectorLayer(VectorMapFilter? filter = null)
    : VectorTileMapLayer(null, filter)
{
    public override VectorTileFeature? CreateFeature(IEnumerable<Mapbox.Vector.Tile.VectorTileFeature> features,
        TilePoint point, float zoom)
    {
        return null;
    }
}