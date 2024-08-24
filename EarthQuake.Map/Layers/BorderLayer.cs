using EarthQuake.Core;
using EarthQuake.Core.TopoJson;
using SkiaSharp;
using System.Diagnostics;

namespace EarthQuake.Map.Layers
{
    /// <summary>
    /// 地物の境界線を描画するレイヤー
    /// </summary>
    /// <param name="polygons">ポリゴン</param>
    public class BorderLayer(CalculatedBorders? polygons) : MapLayer
    {
        private SKPath?[][] buffer = [];
        private Point[][][]? data = polygons?.Points;
        private readonly bool copy;
        public BorderLayer(BorderLayer copySource) : this(polygons: null)
        {
            
            copy = true;
            buffer = copySource.buffer;
        }
        
        private protected override void Initialize(GeomTransform geo)
        {
            var sw = Stopwatch.StartNew();
            if (!copy)
            {
                if (data is not null)
                {
                    buffer = new SKPath?[data.Length][];
                    for (var i1 = 0; i1 < data.Length; i1++)
                    {
                        // ズームレベルごとに実行される
                        var p = data[i1];
                        var paths = new SKPath?[p.Length];
                        for (var i = 0; i < paths.Length; i++)
                        {
                            var points = p[i];
                            if (points.Length < 2)
                            {
                                paths[i] = null;
                                continue;
                            }
                            SKPath path = new();
                            
                            
                            path.MoveTo(geo.Translate(points[0]));
                            for (var j = 1; j < points.Length; j++) path.LineTo(geo.Translate(points[j]));
                            
                            paths[i] = path;
                        }
                        buffer[i1] = paths;
                    }

                }
            }
            sw.Stop();
            Debug.WriteLine($"Border: {sw.ElapsedMilliseconds}ms");
            data = null; // データを解放
            
        }

        private static int GetIndex(float scale) => LandLayer.GetIndex(scale);
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            var index = GetIndex(scale);
            using var paint = new SKPaint();
            paint.Color = SKColors.Gray;
            paint.Style = SKPaintStyle.Stroke;
            paint.IsAntialias = true;
            paint.IsStroke = true;


            foreach (var e in buffer[index]) {
                if (e is null) continue;
                if (e.Bounds.IntersectsWith(bounds)) canvas.DrawPath(e, paint);
            }
        }
    }
}
