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
    public class BorderLayer(TopoJson? json) : ShapeLayer(json, null)
    {
        private readonly SKPath[][] buffer = [[],[],[],[],[],[]];
        private readonly bool copy = false;
        public bool DrawCoast { get; set; } = false;
        public bool DrawCity { get; set; } = true;
        public BorderLayer(BorderLayer copySource) : this(json: null)
        {
            
            copy = true;
            buffer = copySource.buffer;
        }
        
        private protected override void Initialize(GeoTransform geo)
        {
            
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
                            _ => (i - 1) * (i)
                        };
                        Data.AddAllLine(out var coast, out var pref, out var area, out var city, geo, geo.GeometryType, i == 0);
                        buffer[i] = [coast, pref, area, city];
                    }

                }
            }
            
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
            if (DrawCity) canvas.DrawPath(paths[3], paint);
            paint.Color = SKColors.DarkGray;
            canvas.DrawPath(paths[2], paint);
            canvas.DrawPath(paths[1], paint);
            if (DrawCoast) canvas.DrawPath(paths[0], paint);
        }
    }
}
