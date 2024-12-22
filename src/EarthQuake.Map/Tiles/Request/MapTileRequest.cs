using SkiaSharp;

namespace EarthQuake.Map.Tiles.Request;

/// <summary>
/// マップのリクエスト
/// </summary>
/// <param name="point">取得点</param>
/// <param name="tilePoint">タイル上の点</param>
/// <param name="url">送信するURL</param>
public abstract class MapTileRequest(SKPoint point, TilePoint tilePoint, string url) : MapRequest
{
    protected SKPoint Point { get; } = point;
    public TilePoint TilePoint { get; } = tilePoint;
    public string Url { get; } = url;
    protected float Zoom => MathF.Pow(2, TilePoint.Z);
    public abstract object GetAndParse(Stream? data);

    public override string ToString()
    {
        return Url;
    }
}