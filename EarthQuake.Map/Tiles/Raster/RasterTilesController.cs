using EarthQuake.Map.Tiles.Request;
using SkiaSharp;

namespace EarthQuake.Map.Tiles.Raster;

/// <summary>
/// ラスタータイルを読み込むためのコントローラー
/// </summary>
/// <param name="url">ソースURL</param>
public class RasterTilesController(string url) : MapTilesController<RasterTile>(url)
{
    private class RasterTileRequest(SKPoint point, TilePoint tilePoint, string url)
        : MapTileRequest(point, tilePoint, url)
    {
        public override object GetAndParse(Stream? data) =>
            new RasterTile(Point, Zoom, data is null ? null : SKImage.FromEncodedData(data));
    }

    private protected override MapTileRequest GenerateRequest(SKPoint point, TilePoint tilePoint)
    {
        return new RasterTileRequest(point, tilePoint, GenerateUrl(Url, tilePoint))
        {
            Finished = (request, result) =>
            {
                if (request is not RasterTileRequest req || result is not RasterTile tile) return;
                Tiles.Put(req.TilePoint, tile);

                OnUpdate?.Invoke();
            }
        };
    }

    private protected override bool RequestExists(MapRequest request, TilePoint tilePoint) =>
        request is RasterTileRequest req && req.TilePoint.Equals(tilePoint);
}

public record RasterTile(SKPoint LeftTop, float Zoom, SKImage? Image) : IDisposable
{
    public void Dispose()
    {
        Image?.Dispose();
        GC.SuppressFinalize(this);
    }
}