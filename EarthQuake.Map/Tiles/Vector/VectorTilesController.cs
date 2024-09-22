using SkiaSharp;
using EarthQuake.Map.Tiles.Request;

namespace EarthQuake.Map.Tiles.Vector;

/// <summary>
/// ベクトルタイルのコントローラー
/// </summary>
/// <param name="url"></param>
/// <param name="styles"></param>
public class VectorTilesController(string url, VectorMapStyles styles) : MapTilesController<VectorTile>(url)
{
    private class VectorTileRequest(SKPoint point, TilePoint tilePoint, string url, VectorMapStyles styles) : MapTileRequest(point, tilePoint, url)
    {
        public override object GetAndParse(Stream? data) => data is null ? new VectorTile(null) :
            new VectorTile(styles.ParsePaths(data, TilePoint));
    }

    private protected override MapTileRequest GenerateRequest(SKPoint point, TilePoint tilePoint)
    {
        return new VectorTileRequest(point, tilePoint, GenerateUrl(Url, tilePoint), styles)
        {
            Finished = (request, result) =>
            {
                if (request is not VectorTileRequest req || result is not VectorTile tile) return;
                lock (Tiles)
                {
                    Tiles.Put(req.TilePoint, tile);
                }

                OnUpdate?.Invoke();
            }
        };
    }

    private protected override bool RequestExists(MapRequest request, TilePoint tilePoint)
    {
        return request is VectorTileRequest req && req.TilePoint.Equals(tilePoint);
    }
}

public record VectorTile(VectorTileFeature[]? Vertices) : IDisposable
{
    public void Dispose()
    {
        if (Vertices is null) return;
        foreach (var vertex in Vertices)
        {
            vertex.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
