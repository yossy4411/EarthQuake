using SkiaSharp;

namespace EarthQuake.Map.Tiles;

public class VectorTilesController(string url, VectorMapStyles styles) : MapTilesController<VectorTile>(url)
{
    private protected override async Task<VectorTile> GetTile(HttpClient client, SKPoint point, TilePoint point1)
    {
        var response = await client.GetAsync(GenerateUrl(Url, point1.X, point1.Y, point1.Z));
        if (!response.IsSuccessStatusCode) return new VectorTile(point, 0, null);
        await using var network = await response.Content.ReadAsStreamAsync();
        return new VectorTile(point, MathF.Pow(2, point1.Z), styles.ParsePaths(network, point1));
    }
}
public record VectorTile(SKPoint LeftTop, float Zoom, VectorTileFeature[]? Vertices);
