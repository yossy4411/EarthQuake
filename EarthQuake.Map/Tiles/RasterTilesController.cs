using SkiaSharp;

namespace EarthQuake.Map.Tiles;

public class RasterTilesController(string url) : MapTilesController<RasterTile>(url)
{ 
    

    private static async Task<SKImage?> LoadBitmapFromUrlAsync(HttpClient webClient, string url)
    {
        var response = await webClient.GetAsync(url);

        if (!response.IsSuccessStatusCode) return null;
        await using var network = await response.Content.ReadAsStreamAsync();
        return SKImage.FromEncodedData(network);
    }
    private protected override async Task<RasterTile> GetTile(HttpClient client, SKPoint point, TilePoint point1)
    {
        var bitmap = await LoadBitmapFromUrlAsync(client, GenerateUrl(Url, point1.X, point1.Y, point1.Z));
        return new RasterTile(point, MathF.Pow(2, point1.Z), bitmap);
    }
}

public record RasterTile(SKPoint LeftTop, float Zoom, SKImage? Image);