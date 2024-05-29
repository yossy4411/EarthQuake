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
    public class BorderLayer(TopoJson? json) : TopoLayer(json, null)
    {
        private readonly IReadOnlyList<(SKPath, SKRect)>[][] buffer = [[],[],[],[],[],[]];
        private readonly bool copy = false;
        public bool DrawCoast { get; set; } = false;
        public bool DrawCity { get; set; } = true;
        public BorderLayer(BorderLayer copySource) : this(json: null)
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
                    for (int i = 0; i < 5; i++)
                    {
                        Data.Simplify = i switch
                        {
                            0 => 0,
                            1 => 0.5,
                            _ => (i - 1) * i
                        };
                        
                        buffer[i] = Data.AddAllLine(geo, geo.GeometryType, i == 0);
                    }

                }
            }
            sw.Stop();
            Debug.WriteLine($"Border: {sw.ElapsedMilliseconds}ms");
            
        }
        private protected int GetIndex(float scale) => Math.Max(0, Math.Min((int)(-Math.Log(scale * 2, 3) + 3.3), buffer.Length - 1));
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            var paths = buffer[GetIndex(scale)];
            if (paths.Length == 0) return;
            using var paint = new SKPaint()
            {
                Color = SKColors.Gray,
                Style = SKPaintStyle.Stroke,
            };
            void draw((SKPath, SKRect) x)
            {
                if (bounds.IntersectsWith(x.Item2))
                    canvas.DrawPath(x.Item1, paint);
            }
            foreach (var e in paths[3]) draw(e);
            paint.Color = SKColors.DarkGray;
            foreach (var e in paths[2]) draw(e);
            foreach (var e in paths[1]) draw(e);
            if (DrawCoast) foreach (var e in paths[0]) draw(e);
        }
    }
}
