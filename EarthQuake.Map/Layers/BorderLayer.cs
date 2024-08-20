using EarthQuake.Core;
using EarthQuake.Core.TopoJson;
using SkiaSharp;
using System.Diagnostics;

namespace EarthQuake.Map.Layers
{
    /// <summary>
    /// 境を表示するためのレイヤー
    /// </summary>
    /// <param name="json"></param>
    public class BorderLayer(CalculatedBorders? polygons) : MapLayer
    {
        private record Path(SKPath[] Paths, Core.TopoJson.Index[] Indices)
        {
            public readonly bool IsCoast = Indices.Length <= 3;
            public readonly bool IsCity = Indices.Length <= 2;
        }
        private Path[] buffer = [];
        private Border[]? data = polygons?.Points;
        private readonly bool copy = false;
        public bool DrawCoast { get; set; } = false;
        public bool DrawCity { get; set; } = true;
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
                    buffer = new Path[data.Length];
                    for (var i1 = 0; i1 < data.Length; i1++)
                    {
                        var p = data[i1]!;
                        SKPath[] paths = new SKPath[p.Points.Length];
                        for (var i = 0; i < paths.Length; i++)
                        {
                            SKPath path = new();
                            var points = p.Points[i];
                            path.MoveTo(geo.Translate(points[0]));
                            for (var j = 1; j < points.Length; j++) path.LineTo(geo.Translate(points[j]));
                            paths[i] = path;
                        }
                        buffer[i1] = new(paths, p.ContainedIndices);
                    }

                }
            }
            sw.Stop();
            Debug.WriteLine($"Border: {sw.ElapsedMilliseconds}ms");
            data = null; // データを解放
            
        }
        public static int GetIndex(float scale) => LandLayer.GetIndex(scale);
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            var index = GetIndex(scale);
            using var paint = new SKPaint()
            {
                Color = SKColors.Gray,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
            };
            void draw(Path x)
            {
                if (bounds.IntersectsWith(x.Paths[index].Bounds))
                    canvas.DrawPath(x.Paths[index], paint);
            }
            foreach (var e in buffer) {
                if (e.IsCity)
                {
                    if (index == 0 && DrawCity)
                        draw(e);
                    continue;
                }
                if (e.IsCoast)
                {
                    if (DrawCoast)
                        draw(e);
                    continue;
                }
                draw(e);
                     
            }
        }
    }
}
