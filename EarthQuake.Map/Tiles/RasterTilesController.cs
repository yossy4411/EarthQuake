using EarthQuake.Map.Tiles.Request;
using SkiaSharp;

namespace EarthQuake.Map.Tiles;

public class RasterTilesController(string url) : MapTilesController<RasterTile>(url)
{
    private class RasterTileRequest(SKPoint point, TilePoint tilePoint, string url) : MapTileRequest(point, tilePoint, url)
    {
        public override object GetAndParse(Stream data) => new RasterTile(Point, Zoom, SKImage.FromEncodedData(data));
    }

    private protected override MapTileRequest GenerateRequest(SKPoint point, TilePoint tilePoint)
    {
        return new RasterTileRequest(point, tilePoint, GenerateUrl(Url, tilePoint))
        {
            Finished = (request, result) =>
            {
                if (request is not RasterTileRequest req || result is not RasterTile tile) return;
                lock (Tiles)
                {
                    Tiles.Put(req.TilePoint, tile);
                }
            }
        };
    }

    private protected override bool RequestExists(MapRequest request, TilePoint tilePoint) =>
        request is RasterTileRequest req && req.TilePoint == tilePoint;
}

public record RasterTile(SKPoint LeftTop, float Zoom, SKImage? Image);