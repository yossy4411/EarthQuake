using EarthQuake.Core;
using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Core.TopoJson;
using EarthQuake.Map.Colors;
using SkiaSharp;
using System.Diagnostics;

namespace EarthQuake.Map.Layers
{
    public class LandLayer(CalculatedPolygons? polygons) : MapLayer
    {
        public bool Draw { get; set; } = true;
        private protected SKColor[]? colors;
        private Polygon[]? data = polygons?.Points;
        private protected TopoLayer.Polygon[] buffer = [];
        private readonly string[]? names = polygons?.Names;
        public bool AutoFill { get; set; } = false;
        private readonly bool copy = false;
        public LandLayer(LandLayer copySource) : this(polygons: null)
        {
            names = copySource.names;
            buffer = copySource.buffer;
            copy = true;
        }
        private protected override void Initialize(GeomTransform geo)
        {
            Stopwatch sw = Stopwatch.StartNew();

            if (copy || data is null) return;
            buffer = new TopoLayer.Polygon[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                Polygon p = data[i];
                Point[][] innerPoints = p.Points;
                SKVertices[] vertices = new SKVertices[innerPoints.Length];
                for (int j = 0; j < innerPoints.Length; j++)
                {
                    
                    Point[] points = innerPoints[j];
                    SKPoint[] skpoints = new SKPoint[points.Length];
                    for (int k = 0; k < points.Length; k++)
                    {
                        skpoints[k] = geo.Translate(points[k]);
                    }


                    vertices[j] = SKVertices.CreateCopy(SKVertexMode.Triangles, skpoints, null);
                }
                SKPoint p1 = geo.Translate(p.MinX, p.MaxY);
                SKPoint p2 = geo.Translate(p.MaxX, p.MinY);
                buffer[i] = new TopoLayer.Polygon(
                        vertices,
                        new SKRect(p1.X, p2.Y, p2.X, p1.Y)
                    );
            }
            data = null;
            sw.Stop();
            Debug.WriteLine($"{GetType().Name}: {sw.ElapsedMilliseconds}ms");
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
                        else colors[i] = SKColors.Empty;
                    }
                }

            }
        }
        public void Reset()
        {
            colors = null;
        }
        public static int GetIndex(float scale)
        => Math.Max(0, Math.Min((int)(-Math.Log(scale * 2, 3) + 3.3), 5));

        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {

            int index = GetIndex(scale);
            if (Draw)
            {
                using SKPaint paint = new();
                for (int i = 0; i < buffer.Length; i++)
                {
                    var poly = buffer[i];
                    if (!poly.Rect.IntersectsWith(bounds) && false) continue;
                    SKVertices? polygon = poly.Vertices[index];
                    if (AutoFill)
                    {
                        paint.Color = colors?[i] ?? SKColors.DarkGreen;
                        canvas.DrawVertices(polygon, SKBlendMode.Clear, paint);
                    }
                    else
                    {
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
}
