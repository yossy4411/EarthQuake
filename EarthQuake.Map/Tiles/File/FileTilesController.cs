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

            var request = new FillFileTileRequest(file.Points.Points, file.Filling[layerName].Indices[index],
                file.Points.Transform, zoom)
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
    
    public void ClearCaches()
    {
        lock (Tiles)
        {
            foreach (var (_, value) in Tiles)
            {
                value.Dispose();
            }
            Tiles.Clear();
        }
    }
    
    private class FillFileTileRequest(IntPoint[][] points, int[][] indices, Transform transform, int zoom) : FileTileRequest
    {
        public override SKVertices GetAndParse()
        {
            var tess = new Tess
            {
                NoEmptyPolygons = true
            };
            foreach (var index in indices)
            {
                var re = index.SelectMany(i => i >= 0
                    ? ToVertex(points[i], zoom)
                    : Enumerable.Reverse(ToVertex(points[GeomTransform.RealIndex(i)], zoom))).ToList();
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
        
        private List<ContourVertex> ToVertex(IntPoint[] intPoints, int zoomV)
        {
            var zoomFactor = zoomV * zoomV * 4f;  // 平方で計算
            List<ContourVertex> list = new(intPoints.Length);
            var last = GeomTransform.Translate(transform.ToPoint(intPoints[0]));
            list.Add(new ContourVertex(new Vec3(last.X, last.Y, 0)));
            for (var i = 1; i < intPoints.Length; i++)
            {
                var intPoint = intPoints[i];
                var screen = GeomTransform.Translate(transform.ToPoint(intPoint));
                var dx = screen.X - last.X;
                var dy = screen.Y - last.Y;
                if (dx * dx + dy * dy < zoomFactor && i != intPoints.Length - 1) continue;  // この距離以下は無視 (zoom が大きいほど無視する距離が大きくなる)
                list.Add(new ContourVertex(new Vec3(screen.X, screen.Y, 0)));
                last = screen;
            }
            return list;
        }
    }
}