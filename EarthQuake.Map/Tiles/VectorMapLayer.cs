using SkiaSharp;

namespace EarthQuake.Map.Tiles;

public abstract class VectorTileMapLayer (
    VectorMapLayerType type,
    string? source = null,
    VectorMapFilter? filter = null)
{
    public VectorMapLayerType Type { get; } = type;
    public string? Source { get; } = source;
    
    public bool IsVisible(IReadOnlyCollection<KeyValuePair<string, string>> values)
    {
        return filter?.Invoke(new Dictionary<string, string>(values)) ?? true;
    }
}

public delegate bool VectorMapFilter(IEnumerable<KeyValuePair<string, string>> values);

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
}

public class VectorFillLayer(string source, SKColor fillColor, VectorMapFilter? filter = null)
    : VectorTileMapLayer(VectorMapLayerType.Fill, source, filter)
{
    public SKColor FillColor { get; init; } = fillColor;
}

public class VectorLineLayer(string source, SKColor lineColor, float lineWidth, VectorMapFilter? filter = null)
    : VectorTileMapLayer(VectorMapLayerType.Line, source, filter)
{
    public SKColor LineColor { get; } = lineColor;
    public float LineWidth { get; } = lineWidth;
    public SKStrokeCap StrokeCap { get; init; } = SKStrokeCap.Butt;
    public SKStrokeJoin StrokeJoin { get; init; } = SKStrokeJoin.Miter;
    public SKPathEffect? PathEffect { get; init; }
}

public class VectorSymbolLayer(string source, string textField, SKColor textColor, float textSize, VectorMapFilter? filter = null)
    : VectorTileMapLayer(VectorMapLayerType.Symbol, source, filter)
{
    public string TextField { get; } = textField;
    public SKColor TextColor { get; } = textColor;
    public float TextSize { get; } = textSize;
}

public class EmptyVectorLayer(VectorMapFilter? filter = null)
    : VectorTileMapLayer(VectorMapLayerType.Background, null, filter);
    