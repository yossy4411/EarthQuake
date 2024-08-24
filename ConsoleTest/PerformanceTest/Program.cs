using System.Diagnostics;
using SkiaSharp;

namespace PerformanceTest;

public static class Program
{
    private static void Main()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        for (var i = 0; i < 100; i++)
        {
            using var surface = SKSurface.Create(new SKImageInfo(800, 600));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            // 仮想的に画面外にある SKVertices
            var vertices = SKVertices.CreateCopy(
                SKVertexMode.TriangleFan,
                [new SKPoint(1000, 1000), new SKPoint(1200, 1000), new SKPoint(1100, 1300)],
                null,
                null,
                null
            );
            using var paint = new SKPaint();
            paint.Color = SKColors.Red;
            canvas.DrawVertices(vertices, SKBlendMode.Color, paint);

            // 画像を保存するための処理など
            using var image = surface.Snapshot();
            using var data = image.Encode();
            using var stream = File.OpenWrite("output.png");
            data.SaveTo(stream);
        }

        stopwatch.Stop();
        Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
    }
}