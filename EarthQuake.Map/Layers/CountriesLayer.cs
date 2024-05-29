using EarthQuake.Core;
using EarthQuake.Core.GeoJson;
using LibTessDotNet;
using SkiaSharp;
using System.Diagnostics;
using static EarthQuake.Map.Layers.TopoLayer;

namespace EarthQuake.Map.Layers
{
    public class CountriesLayer(GeoJson geojson) : MapLayer
    {
        public GeoJson? Data = geojson;
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
        private protected override void Initialize(GeomTransform geo)
        {
            var sw = Stopwatch.StartNew();
            if (Data is not null && Data.Features is not null)
            {
                foreach (GeoJson.Feature feature in Data.Features)
                {
                    float minX = float.MaxValue;
                    float maxX = float.MinValue;
                    float minY = float.MaxValue;
                    float maxY = float.MinValue;
                    Tess tess = new();
                    for (int i = 0; i < feature.Geometry?.Coordinates.Length; i++)
                    {
                        feature.Geometry?.AddVertex(tess, geo, i, ref minX, ref minY, ref maxX, ref maxY);
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
            Data = null;
            sw.Stop();
            Debug.WriteLine($"World: {sw.ElapsedMilliseconds}ms");
        }
    }
}
