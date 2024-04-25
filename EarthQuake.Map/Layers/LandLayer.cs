using EarthQuake.Core;
using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Core.TopoJson;
using EarthQuake.Map.Colors;
using LibTessDotNet;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Map.Layers
{
    public class LandLayer(TopoJson json, string layerName = "info") : TopoLayer(json, layerName) // ["eew", "info", "city"]
    {
        public bool Draw { get; set; } = true;
        private protected SKColor[]? colors;
        private protected readonly Polygon[][] buffer = [[],[],[],[],[],[]];
        private string[]? names;
        public bool AutoFill = false;

        private protected override void Initialize(GeomTransform geo)
        {
            if (Data is not null && Data.Geometries is not null)
            {
                for (int i2 = 0; i2 < 6; i2++)
                {
                    Data.Simplify = i2 switch
                    {
                        0 => 0,
                        1 => 0.5,
                        _ => (i2 - 1) * (i2)
                    };
                    List<Polygon> polygons = [];
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
                    buffer[i2] = [..polygons];
                }
            }
            names = [..Data?.Geometries?.Select(x=>x.Properties?.Name)];
        }
        public void SetInfo(PQuakeData data)
        {
            if (names is not null && data?.Points is not null)
            {
                colors = new SKColor[names.Length];
                for (int i = 0; i < names.Length; i++)
                {
                    var name = names[i];
                    if (name is not null)
                    {
                        var a = data.Points.Where(x => x.Addr.StartsWith(name))
                            .FirstOrDefault();
                        if (a is not null) colors[i] = Kiwi3Color.GetColor(a.Scale);
                        else if (AutoFill) colors[i] = SKColors.DarkGreen;
                    }
                }

            }
        }
        public void Reset()
        {
            colors = null;
        }
        private protected int GetIndex(float scale) => Math.Max(0, Math.Min((int)(-Math.Log(scale * 2, 3) + 3.3), buffer.Length - 1)); 
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            
            var polygons = buffer[GetIndex(scale)];
            if (Draw)
            {
                using SKPaint paint = new();
                for (int i = 0; i < polygons.Length; i++)
                {
                    Polygon poly = polygons[i];
                    if (!poly.Rect.IntersectsWith(bounds)) continue;
                    SKVertices? polygon = poly.Vertices;
                    
                    //paint.Color = colors?[i] ?? SKColors.DarkGreen;
                    if (colors?[i] is not null)
                        canvas.DrawVertices(polygon, SKBlendMode.Clear, paint);
                }
            }
            
        }
        
    }
}
