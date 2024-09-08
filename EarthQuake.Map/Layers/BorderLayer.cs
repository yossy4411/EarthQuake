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
        private SKPath[][] buffer = [];
        private SKPoint[][][]? data = polygons?.Points;
        private readonly int[][][]? indices = polygons?.Indices;
        private readonly bool copy;
        public BorderLayer(BorderLayer copySource) : this(polygons: null)
        {
            
            copy = true;
            buffer = copySource.buffer;
        }
        
        private protected override void Initialize()
        {
            var sw = Stopwatch.StartNew();
            if (!copy)
            {
                if (data is not null && indices is not null)
                {
                    buffer = new SKPath[data.Length][];
                    for (var d = 0; d < data.Length; d++)
                    {
                        // ズームレベルごとに実行される
                        var points = data[d];
                        var paths = new SKPath[indices.Length];
                        for (var i = 0; i < indices.Length; i++)
                        {
                            var path = new SKPath();
                            for (var j = 0; j < indices[i].Length; j++)
                            {
                                var indices1 = indices[i][j];
                                if (indices1.Length < 2) continue;

                                var index1 = indices1[0];
                                if (index1 < 0)
                                {
                                    // 逆方向からアクセス
                                    var points1 = points[GeomTransform.RealIndex(index1)];
                                    path.MoveTo(GeomTransform.Translate(points1[^1]));
                                    for (var i1 = points1.Length - 2; i1 >= 0; i1--)
                                    {
                                        path.LineTo(GeomTransform.Translate(points1[i1]));
                                    }
                                }
                                else
                                {
                                    // 正方向からアクセス
                                    var points1 = points[index1];
                                    path.MoveTo(GeomTransform.Translate(points1[0]));
                                    for (var i1 = 1; i1 < points1.Length; i1++)
                                    {
                                        path.LineTo(GeomTransform.Translate(points1[i1]));
                                    }
                                }
                                
                                for (var i1 = 1; i1 < indices1.Length; i1++)
                                {
                                    var index = indices1[i1];
                                    if (index < 0)
                                    {
                                        // 逆方向からアクセス
                                        var points1 = points[GeomTransform.RealIndex(index)];
                                        for (var i2 = points1.Length - 1; i2 >= 0; i2--)
                                        {
                                            path.LineTo(GeomTransform.Translate(points1[i2]));
                                        }
                                    }
                                    else
                                    {
                                        // 正方向からアクセス
                                        var points1 = points[index];
                                        foreach (var t in points1)
                                        {
                                            path.LineTo(GeomTransform.Translate(t));
                                        }
                                    }
                                }
                                
                            }

                            paths[i] = path;
                        }
                        buffer[d] = paths;
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
                if (e.Bounds.IntersectsWith(bounds)) canvas.DrawPath(e, paint);
            }
        }
    }
}
