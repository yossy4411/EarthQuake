using EarthQuake.Core;
using EarthQuake.Core.TopoJson;
using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private readonly Border[]? Data = polygons?.Points;
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
            Stopwatch sw = Stopwatch.StartNew();
            if (!copy)
            {
                if (Data is not null)
                {
                    buffer = new Path[Data.Length];
                    for (int i1 = 0; i1 < Data.Length; i1++)
                    {
                        Border p = Data[i1]!;
                        SKPath[] paths = new SKPath[p.Points.Length];
                        for (int i = 0; i < paths.Length; i++)
                        {
                            SKPath path = new();
                            Point[] points = p.Points[i];
                            path.MoveTo(geo.Translate(points[0]));
                            for (int j = 1; j < points.Length; j++) path.LineTo(geo.Translate(points[j]));
                            paths[i] = path;
                        }
                        buffer[i1] = new(paths, p.ContainedIndice);
                    }

                }
            }
            sw.Stop();
            Debug.WriteLine($"Border: {sw.ElapsedMilliseconds}ms");
            
        }
        public static int GetIndex(float scale) => LandLayer.GetIndex(scale);
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            int index = GetIndex(scale);
            using var paint = new SKPaint()
            {
                Color = SKColors.Gray,
                Style = SKPaintStyle.Stroke,
            };
            void draw(Path x)
            {
                if (bounds.IntersectsWith(x.Paths[index].Bounds))
                    canvas.DrawPath(x.Paths[index], paint);
            }
            foreach (var e in buffer) { 
                if (!e.IsCity || index == 0)
                    draw(e); 
            }
        }
    }
}
