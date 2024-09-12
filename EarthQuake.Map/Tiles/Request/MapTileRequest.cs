using SkiaSharp;

namespace EarthQuake.Map.Tiles.Request;

public abstract class MapTileRequest(SKPoint point, TilePoint tilePoint, string url) : MapRequest
{
    protected SKPoint Point { get; } = point;
    public TilePoint TilePoint { get; } = tilePoint;
    public string Url { get; } = url;
    protected float Zoom => MathF.Pow(2, TilePoint.Z);
    public abstract object GetAndParse(Stream data);
}