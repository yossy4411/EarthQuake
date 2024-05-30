using Avalonia.Controls.Shapes;
using EarthQuake.Core;
using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Core.TopoJson;
using EarthQuake.Map.Colors;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Map.Layers
{
    public class CitiesLayer(CalculatedPolygons json) : LandLayer(json)
    {
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            int index = GetIndex(scale);
            if (Draw)
            {
                using SKPaint paint = new();
                for (int i = 0; i < buffer.Length; i++)
                {
                    SKVertices? polygon = buffer[i].Vertices[index];
                    if (colors?[i] is not null)
                    {
                        paint.Color = colors[i];
                        canvas.DrawVertices(polygon, SKBlendMode.Clear, paint);
                    }
                }
            }
        }
    }
}
