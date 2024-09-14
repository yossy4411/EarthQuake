using EarthQuake.Core;
using EarthQuake.Core.TopoJson;
using EarthQuake.Map.Tiles.Request;
using LibTessDotNet;
using SkiaSharp;

namespace EarthQuake.Map.Tiles.File;

public class FileTilesController(PolygonsSet file, string layerName)
{
    private Dictionary<(int, int), SKVertices> Tiles { get; } = new();
    
    public SKVertices? TryGetTile(int zoom, int index)
    {
        lock (Tiles)
        {
            if (Tiles.TryGetValue((zoom, index), out var tile))
            {
                return tile;
            }

            var request = new FillFileTileRequest(file.Points.Points[zoom], file.Filling[layerName].Indices[index],
                file.Points.Transform)
            {
                Finished = (request, result) =>
                {
                    if (request is not FileTileRequest || result is not SKVertices vertices) return;
                    lock (Tiles)
                    {
                        Tiles.Add((zoom, index), vertices);
                    }
                }
            };
            MapRequestHelper.AddRequest(request);
        }

        return null;
    }
    private class FillFileTileRequest(IntPoint[][] points, int[][] indices, Transform transform) : FileTileRequest
    {
        public override SKVertices GetAndParse()
        {
            var tess = new Tess
            {
                NoEmptyPolygons = true
            };
            foreach (var index in indices)
            {
                var re = index.SelectMany(i => i >= 0 ? points[i].Select(ToVertex) :
                    points[GeomTransform.RealIndex(i)].Select(ToVertex).Reverse()).ToList();
                tess.AddContour(re);
            }
            tess.Tessellate(WindingRule.Positive);
            var tessPoints = new SKPoint[tess.ElementCount * 3];
            for (var j = 0; j < tessPoints.Length; j++)
            { 
                tessPoints[j] = new SKPoint(tess.Vertices[tess.Elements[j]].Position.X, 
                    tess.Vertices[tess.Elements[j]].Position.Y);
            }
            return SKVertices.CreateCopy(SKVertexMode.Triangles, tessPoints, null);
        }

        private ContourVertex ToVertex(IntPoint point)
        {
            var screen = GeomTransform.Translate(transform.ToPoint(point));
            return new ContourVertex(new Vec3(screen.X, screen.Y, 0));
        }
    }
}