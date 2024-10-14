#if false
using EarthQuake.Map.Tiles.Vector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


using var stream = File.OpenRead("osm.json");
using var reader = new StreamReader(stream);
var style = VectorMapStyles.LoadGLJson(reader);
Console.WriteLine(style.Name);

#elif true
using SkiaSharp;


var skVertices = SKVertices.CreateCopy(
    SKVertexMode.TriangleStrip,
    [
        new SKPoint(50, 50),  
        new SKPoint(150, 50), 
        new SKPoint(150, 150),
        new SKPoint(100, 150),
        new SKPoint(50, 50)
    ],
    null
);

// SKCanvasに描画
using var surface = SKSurface.Create(new SKImageInfo(200, 200));
var canvas = surface.Canvas;
canvas.Clear(SKColors.White);

using (var paint = new SKPaint())
{
    paint.Color = SKColors.Black;
    paint.StrokeWidth = 2;
    paint.IsAntialias = true;
    paint.Style = SKPaintStyle.Stroke;

    // SKVerticesを描画
    canvas.DrawVertices(skVertices, SKBlendMode.Src, paint);
}

// 画像を保存 (例としてPNG形式)
using (var image = surface.Snapshot())
using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
using (var stream = File.OpenWrite("vertices_lines.png"))
{
    data.SaveTo(stream);
}

#endif
