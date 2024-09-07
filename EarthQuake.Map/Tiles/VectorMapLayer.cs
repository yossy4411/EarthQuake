using SkiaSharp;

namespace EarthQuake.Map.Tiles;

public abstract class VectorTileMapLayer (
    VectorMapLayerType type,
    string? source = null,
    VectorMapFilter? filter = null)
{
    public VectorMapLayerType Type { get; } = type;
    public string? Source { get; } = source;
    
    public bool IsVisible(Dictionary<string, object> values)
    {
        return filter?.Invoke(values) ?? true;
    }
    
    public abstract VectorTileFeature? CreateFeature(IEnumerable<Mapbox.Vector.Tile.VectorTileFeature> features, TilePoint point);
}

public delegate bool VectorMapFilter(Dictionary<string, object> values);

public enum VectorMapLayerType
{
    Background, // 背景
    Fill,   // 塗りつぶし
    Line,   // 線
    Symbol // テキスト
}

public class VectorBackgroundLayer(SKColor backgroundColor)
    : VectorTileMapLayer(VectorMapLayerType.Background)
{
    public SKColor BackgroundColor { get; init; } = backgroundColor;
    public override VectorTileFeature? CreateFeature(IEnumerable<Mapbox.Vector.Tile.VectorTileFeature> features, TilePoint point)
    {
        return null;
    }
}

public class VectorFillLayer(string source, SKColor fillColor, VectorMapFilter? filter = null)
    : VectorTileMapLayer(VectorMapLayerType.Fill, source, filter)
{
    public SKColor FillColor { get; init; } = fillColor;
    public override VectorTileFeature CreateFeature(IEnumerable<Mapbox.Vector.Tile.VectorTileFeature> features, TilePoint point)
    {
        return new VectorFillFeature(features, point);
    }
}

public class VectorLineLayer(string source, SKColor lineColor, float lineWidth, VectorMapFilter? filter = null)
    : VectorTileMapLayer(VectorMapLayerType.Line, source, filter)
{
    public SKColor LineColor { get; } = lineColor;
    public float LineWidth { get; } = lineWidth;
    public SKStrokeCap StrokeCap { get; init; } = SKStrokeCap.Butt;
    public SKStrokeJoin StrokeJoin { get; init; } = SKStrokeJoin.Miter;
    public SKPathEffect? PathEffect { get; init; }
    public override VectorTileFeature CreateFeature(IEnumerable<Mapbox.Vector.Tile.VectorTileFeature> features, TilePoint point)
    {
        return new VectorLineFeature(features, point);
    }
}

public class VectorSymbolLayer(string source, SKColor textColor, float textSize, VectorMapFilter? filter = null)
    : VectorTileMapLayer(VectorMapLayerType.Symbol, source, filter)
{
    public SKColor TextColor { get; } = textColor;
    public float TextSize { get; } = textSize;
    public override VectorTileFeature? CreateFeature(IEnumerable<Mapbox.Vector.Tile.VectorTileFeature> features, TilePoint point)
    {
        return null; // TODO: 文字の描画の実装
    }
}

public class EmptyVectorLayer(VectorMapFilter? filter = null)
    : VectorTileMapLayer(VectorMapLayerType.Background, null, filter)
{
    public override VectorTileFeature? CreateFeature(IEnumerable<Mapbox.Vector.Tile.VectorTileFeature> features,
        TilePoint point)
    {
        return null;
    }
}
    