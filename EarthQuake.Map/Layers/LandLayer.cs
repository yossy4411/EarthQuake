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
        private protected SKColor[]? Colors;
        private Polygons[]? data = polygons?.Points;
        private protected TopoLayer.Polygon[] Buffer = [];
        private readonly string[]? names = polygons?.Names;
        public bool AutoFill { get; init; } = false;
        private readonly bool copy = false;
        public LandLayer(LandLayer copySource) : this(polygons: null)
        {
            copy = true;
            names = copySource.names;
            Buffer = copySource.Buffer;
        }
        private protected override void Initialize(GeomTransform geo)
        {
            var sw = Stopwatch.StartNew();

            if (copy || data is null) return;
            Buffer = new TopoLayer.Polygon[data.Length];
            for (var i = 0; i < data.Length; i++)
            {
                var p = data[i];
                Point[][] innerPoints = p.Points;
                SKVertices[] vertices = new SKVertices[innerPoints.Length];
                for (var j = 0; j < innerPoints.Length; j++)
                {
                    
                    var points = innerPoints[j];
                    var skpoints = new SKPoint[points.Length];
                    for (var k = 0; k < points.Length; k++)
                    {
                        skpoints[k] = geo.Translate(points[k]);
                    }


                    vertices[j] = SKVertices.CreateCopy(SKVertexMode.Triangles, skpoints, null);
                }
                var p1 = geo.Translate(p.MinX, p.MaxY);
                var p2 = geo.Translate(p.MaxX, p.MinY);
                Buffer[i] = new TopoLayer.Polygon(
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
                Colors = new SKColor[names.Length];
                for (var i = 0; i < names.Length; i++)
                {
                    var name = names[i];
                    if (name is not null)
                    {
                        var a = data.Points.Where(x => x.Addr.StartsWith(name))
                            .FirstOrDefault();
                        if (a is not null) Colors[i] = Kiwi3Color.GetColor(a.Scale);
                        else if (AutoFill) Colors[i] = SKColors.DarkGreen;
                        else Colors[i] = SKColors.Empty;
                    }
                }

            }
        }
        public void Reset()
        {
            Colors = null;
        }
        public static int GetIndex(float scale)
        => Math.Max(0, Math.Min((int)(-Math.Log(scale * 2, 3) + 3.3), 5));

        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {

            var index = GetIndex(scale);
            if (Draw)
            {
                using SKPaint paint = new();
                for (var i = 0; i < Buffer.Length; i++)
                {
                    var poly = Buffer[i];
                    if (!poly.Rect.IntersectsWith(bounds) && false) continue;
                    var polygon = poly.Vertices[index];
                    if (AutoFill)
                    {
                        paint.Color = Colors?[i] ?? SKColors.DarkGreen;
                        canvas.DrawVertices(polygon, SKBlendMode.Clear, paint);
                    }
                    else
                    {
                        if (Colors?[i] is not null)
                        {
                            paint.Color = Colors[i];
                            canvas.DrawVertices(polygon, SKBlendMode.Clear, paint);
                        }
                    }
                    
                }
            }
            
        }
        
    }
}
