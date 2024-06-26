﻿using Avalonia.Controls.Shapes;
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
    public class CitiesLayer(TopoJson json) : LandLayer(json, "city")
    {
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            var polygons = buffer[GetIndex(scale)];
            if (Draw)
            {
                using SKPaint paint = new();
                for (int i = 0; i < polygons.Length; i++)
                {
                    SKVertices? polygon = polygons[i].Vertices;
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
