using Avalonia.Media;
using EarthQuake.Core;
using EarthQuake.Core.TopoJson;
using LibTessDotNet;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Map.Layers
{
    public class CountriesLayersWithTopoJson(TopoJson? json) : ShapeLayer(json, "WB_countries_Admin0_10m")
    {
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            using SKPaint paint = new() { Color = SKColors.Green };
            foreach (Polygon value in polygons)
            {
                SKVertices? polygon = value.Vertices;
                if (!value.Rect.IntersectsWith(bounds)) continue;
                canvas.DrawVertices(polygon, SKBlendMode.Clear, paint);
            }
        }
        private readonly List<Polygon> polygons = [];
        private protected override void Initialize(GeoTransform geo)
        {
            if (Data is not null && Data.Geometries is not null)
            {
                
                Data.Simplify = 0;
                for (int i = 0; i < Data.Geometries.Length; i++)
                {
                    float minX = float.MaxValue;
                    float maxX = float.MinValue;
                    float minY = float.MaxValue;
                    float maxY = float.MinValue;
                    Feature? feature = Data.Geometries[i];
                    Tess tess = new();
                    if (feature.Arcs is not null)
                    {

                        foreach (var polygon in feature.Arcs)
                        {

                            Data.AddVertex(tess, polygon[0], geo, ref minX, ref minY, ref maxX, ref maxY);
                        }
                    }
                    tess.Tessellate(WindingRule.Positive);
                    SKPoint[] points = new SKPoint[tess.ElementCount * 3];
                    for (int j = 0; j < points.Length; j++)
                    {
                        points[j] = new(tess.Vertices[tess.Elements[j]].Position.X, tess.Vertices[tess.Elements[j]].Position.Y);
                    }
                    polygons.Add(new(SKVertices.CreateCopy(SKVertexMode.Triangles, points, null), new(minX, minY, maxX, maxY)));
                }
            }
        }
    }
}
