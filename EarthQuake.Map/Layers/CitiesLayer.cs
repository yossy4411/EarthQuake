using EarthQuake.Core.TopoJson;
using SkiaSharp;

namespace EarthQuake.Map.Layers
{
    public class CitiesLayer(CalculatedPolygons json) : LandLayer(json)
    {
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            var index = GetIndex(scale);
            if (Draw)
            {
                using SKPaint paint = new();
                for (var i = 0; i < Buffer.Length; i++)
                {
                    var polygon = Buffer[i].Vertices[index];
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
