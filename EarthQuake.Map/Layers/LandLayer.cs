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
        private SKColor[]? colors;
        private SKPoint[][][]? data = polygons?.Points;
        private SKVertices[][] buffer = [];
        private readonly string[]? names = polygons?.Names;
        public bool AutoFill { get; init; }
        private readonly bool copy;
        public LandLayer(LandLayer copySource) : this(polygons: null)
        {
            copy = true;
            names = copySource.names;
            buffer = copySource.buffer;
        }
        private protected override void Initialize()
        {
            var sw = Stopwatch.StartNew();

            if (copy || data is null) return;
            buffer = data.Select(p =>
                p.Select(x =>
                        SKVertices.CreateCopy(SKVertexMode.Triangles, x.Select(GeomTransform.Translate).ToArray(),
                            null))
                    .ToArray()).ToArray();
            data = null;
            sw.Stop();
            Debug.WriteLine($"{GetType().Name}: {sw.ElapsedMilliseconds}ms");
        }
        public void SetInfo(PQuakeData quakeData)
        {
            if (names is null || quakeData.Points is null) return;
            colors = new SKColor[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var a = quakeData.Points.FirstOrDefault(x => x.Addr.StartsWith(name));
                if (a is not null) colors[i] = a.Scale.GetKiwi3Color();
                else if (AutoFill) colors[i] = SKColors.DarkGreen;
                else colors[i] = SKColors.Empty;
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

            var index = GetIndex(scale);
            if (!Draw) return;
            using SKPaint paint = new();
            var polygons = buffer[index];
            for (var i = 0; i < polygons.Length; i++)
            {
                var poly = polygons[i];
                if (AutoFill)
                {
                    paint.Color = colors?[i] ?? SKColors.DarkGreen;
                }
                else
                {
                    if (colors?[i] is null) continue;
                    paint.Color = colors[i];
                }

                canvas.DrawVertices(poly, SKBlendMode.SrcOver, paint);
            }

        }
        
    }
}
