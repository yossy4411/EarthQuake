using LibTessDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;


namespace EarthQuake.Core.TopoJson
{
    public class TopoJson
    {

        public int[][][] Arcs { get; set; } = [[[]]];
        public (float, float) Translate(int x, int y)
        {
            return ((float)(x * Transform.Scale[0] + Transform.Translate[0]), (float)(y * Transform.Scale[1] + Transform.Translate[1]));
        }
        public Transform Transform { get; set; } = new();
        public Dictionary<string, Layer> Objects { get; set; } = [];
        public MapData? GetLayer(string layerName)
        {
            var layer = Objects.GetValueOrDefault(layerName);
            return layer is null ? null : new MapData(Arcs, layer, Transform);
        }
        public MapData CreateLayer()
        {
            return new MapData(Arcs, null, Transform);
        }
    }
    public class MapData
    {
        private readonly Layer? _layer;
        private readonly int[][][] _arcs;
        private readonly Transform _transform;
        public double Simplify = 0;
        public enum PolygonType : byte
        {
            None = 1,
            Coast = 2,
            Pref = 4,
            Area = 8,
            City = 16
        }
        internal MapData(int[][][] arcs, Layer? layer, Transform transform)
        {

            _layer = layer;
            _arcs = arcs;
            _transform = transform;
        }
        public void AddVertex(Tess tess, int[] contours)
        {
            List<ContourVertex> result = [];
            foreach (var index in contours)
            {
                var index1 = index >= 0 ? index : -index - 1;

                var coords = _arcs[index1];
                int x = coords[0][0], y = coords[0][1];
                var skPoint = _transform.ToSkPoint(x, y);
                List<ContourVertex> vertices = [new ContourVertex { Position = new Vec3(skPoint.X, skPoint.Y, 0) }];
                for (var i = 1; i < coords.Length; i++)
                {
                    x += coords[i][0];
                    y += coords[i][1];
                    
                    var point = _transform.ToSkPoint(x, y);
                    if (Simplify != 0 && !(SKPoint.Distance(skPoint, point) * 50 >= Simplify | i == coords.Length - 1))
                        continue;
                    vertices.Add(new ContourVertex { Position = new Vec3(point.X, point.Y, 0) });
                    skPoint = point;
                }

                if (index < 0)
                {
                    vertices.Reverse();
                }
                result.AddRange(vertices);
            }
            tess.AddContour(result);
        }
        
        public Point[] GenerateBorder(int index)
        {
            
            List<Point> child = [];
            var index1 = index >= 0 ? index : -index - 1;

            var coords = _arcs[index1];
            int x = coords[0][0], y = coords[0][1];
            var sPoint = _transform.ToPoint(x, y);
            child.Add(sPoint);
            for (var i = 1; i < coords.Length; i++)
            {
                x += coords[i][0];
                y += coords[i][1];

                var point = _transform.ToPoint(x, y);
                if (Simplify != 0 && !(Point.Distance(sPoint, point) * 50 >= Simplify | i == coords.Length - 1))
                    continue;
                child.Add(point);
                sPoint = point;
            }

            if (index < 0)
            {
                child.Reverse();
            } 
            return [..child];
        }

        public SKPath ToPath(int[][] contours)
        {
            SKPath path = new();
            foreach (var contour in contours)
            {
                foreach (var index in contour)
                {
                    List<SKPoint> vertices = [];
                    var index1 = index >= 0 ? index : -index - 1;

                    var coords = _arcs[index1];
                    int x = coords[0][0], y = coords[0][1];
                    var skPoint = _transform.ToSkPoint(x, y);
                    vertices.Add(skPoint);
                    for (var i = 1; i < coords.Length; i++)
                    {
                        x += coords[i][0];
                        y += coords[i][1];

                        var point = _transform.ToSkPoint(x, y);
                        vertices.Add(point);
                    }

                    if (index < 0)
                    {
                        vertices.Reverse();
                    }
                    path.AddPoly(vertices.ToArray());
                }
            }

            return path;
        }

        public string LayerName => _layer?.Name ?? string.Empty;
        public Feature[]? Geometries => _layer?.Geometries;
    }
    public class Transform
    {
        public double[] Scale { get; set; } = [0, 0];
        public double[] Translate { get; set; } = [0, 0];
        public SKPoint ToSkPoint(int x, int y)
        {
            return new SKPoint((float)(x * Scale[0] + Translate[0]), (float)(y * Scale[1] + Translate[1]));
        }
        public Point ToPoint(int x, int y)
        {
            return new Point((float)(x * Scale[0] + Translate[0]), (float)(y * Scale[1] + Translate[1]));
        }

    }

    public class Layer
    {
        public string Name { get; set; } = string.Empty;
        public Feature[] Geometries { get; set; } = [];
    }
    public class Feature
    {
        [JsonConverter(typeof(ArcConverter))]
        public int[][][] Arcs { get; set; } = [];
        public Property? Properties { get; set; }
        private class ArcConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(int[][]) || objectType == typeof(int[][][]);
            }

            public override int[][][]? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
            {
                var array = JArray.Load(reader);
                if (array.First?.First?.Type == JTokenType.Array)
                {
                    return array.ToObject<int[][][]>();
                }
                else
                {
                    return [array.ToObject<int[][]>() ?? [[]]];
                }
            }

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
        public class Property
        {
            public string? Code { get; set; }
            public string? Name { get; set; }
            public string? Namekana { get; set; }
        }
    }

}
