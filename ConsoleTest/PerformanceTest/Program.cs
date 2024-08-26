using System.IO.Compression;
using System.Threading.Channels;
using Mapbox.VectorTile;


using var client = new HttpClient();

var file = await client.GetByteArrayAsync("http://172.18.0.93:8080/data/v3/10/899/406.pbf");


// PBFデータを解凍する
using var compressedStream = new MemoryStream(file);
await using var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress);
using var resultStream = new MemoryStream();
decompressionStream.CopyTo(resultStream);
var decompressed = resultStream.ToArray();

var vectorTile = new VectorTile(decompressed);

var layer = vectorTile.GetLayer("landuse");
// 1個目のフィーチャを取得
var feature = layer.GetFeature(0);
// ポリゴンのジオメトリを取得
var geometry = feature.Geometry<float>();
foreach (var point2d in geometry.SelectMany(x => x))
{
    Console.WriteLine(point2d.X);
    Console.WriteLine(point2d.Y);
    
    // Webメルカトルの位置?に変換
    var x = point2d.X / 4096 * 360 - 180;
    var y = point2d.Y / 4096 * 360 - 180;
    Console.WriteLine(x);
    Console.WriteLine(y);
}