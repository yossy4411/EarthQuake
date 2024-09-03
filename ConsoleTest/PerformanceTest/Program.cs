using System.Diagnostics;
using Mapbox.Vector.Tile;
using SkiaSharp;


var file = new FileStream("3244.pbf", FileMode.Open, FileAccess.Read); // https://cyberjapandata.gsi.go.jp/xyz/experimental_bvmap/13/7189/3244.pbf

// await using var decompressionStream = new GZipStream(file, CompressionMode.Decompress);
var vectorTile = VectorTileParser.Parse(file);

var sw = Stopwatch.StartNew();

foreach (var vectorTileLayer in vectorTile)
{
    var features =
        string.Join("\n\n", vectorTileLayer.VectorTileFeatures.Select(x =>
            $"""
             Attributes:
             {string.Join('\n', x.Attributes.Select(v => $"{v.Key}: {v.Value}"))}
             Type: {x.GeometryType}
             {x.Geometry.SelectMany(p=>p).Count()} Points found.
             """));
    Console.WriteLine($"""
                       Name: {vectorTileLayer.Name}
                       Features: {features}
                       
                       """);
}

sw.Stop();
Console.WriteLine($"{sw.ElapsedMilliseconds}ms elapsed for parsing and printing the vector tile data.");

var paint = new SKPaint();