using SkiaSharp;
using EarthQuake.Map.Tiles.Request;
using PMTiles;
using VectorTiles.Styles;

namespace EarthQuake.Map.Tiles.Vector;

/// <summary>
/// ベクトルタイルのコントローラー
/// </summary>
public class VectorTilesController : MapTilesController<VectorTile>
{
    private readonly VectorMapStyle styles1;
    public PMTilesReader? PMTiles { get; private set; }
    
    /// <summary>
    /// ベクトルタイルのコントローラー
    /// </summary>
    /// <param name="url"></param>
    /// <param name="styles"></param>
    public VectorTilesController(VectorMapStyle styles) : base(styles.Sources.Count == 0 || styles.Sources[0].Url is null ? "unknown url" : styles.Sources[0].Url!, 100)
    {
        styles1 = styles;
        if (styles.Sources.Count == 0) return;
        
        var s = styles.Sources[0].Url;
        

        if (s is not null && s.StartsWith("pmtiles://"))
        {
            // PMTiles の場合、私が作った`PMTiles.NET`のライブラリを使う
            // pmtiles://<url>/{z}/{x}/{y} の形式で指定されているが、必要なのは<url>の部分のみ。
            PMTiles = PMTilesReader.FromUrl(s[10..].Replace("/{z}/{x}/{y}", ""));  // PMTilesReaderを作成する際にWebリクエストを行うため、非同期で実行
        }
    }
    
    internal class VectorTileRequest(SKPoint point, TilePoint tilePoint, string url, VectorMapStyle styles, PMTilesReader? reader)
        : MapTileRequest(point,
            tilePoint, url)
    {
        public PMTilesReader? PMReader { get; } = reader;
        public override VectorTile GetAndParse(Stream? data) => data is null ? new VectorTile(null) :
            new VectorTile(styles.ParsePaths(data, TilePoint));
    }

    private protected override MapTileRequest GenerateRequest(SKPoint point, TilePoint tilePoint)
    {
        return new VectorTileRequest(point, tilePoint, GenerateUrl(Url, tilePoint), styles1, PMTiles)
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
        foreach (var vertex in Vertices ?? [])
        {
            vertex.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
