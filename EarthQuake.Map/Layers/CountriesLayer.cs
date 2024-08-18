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
        private record Polygon(SKVertices Vertices, SKRect Rect);
        public GeoJson? Data = geojson;
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            using SKPaint paint = new() { Color = SKColors.Green };
            foreach (var value in polygons)
            {
                var polygon = value.Vertices;
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
                foreach (var feature in Data.Features)
                {
                    var minX = float.MaxValue;
                    var maxX = float.MinValue;
                    var minY = float.MaxValue;
                    var maxY = float.MinValue;
                    Tess tess = new();
                    for (var i = 0; i < feature.Geometry?.Coordinates.Length; i++)
                    {
                        feature.Geometry?.AddVertex(tess, geo, i, ref minX, ref minY, ref maxX, ref maxY);
                        tess.Tessellate(WindingRule.Positive);
                        var points = new SKPoint[tess.ElementCount * 3];
                        for (var j = 0; j < points.Length; j++)
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
