using LibTessDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;
// ReSharper disable ClassNeverInstantiated.Global


namespace EarthQuake.Core.TopoJson
{
    public class TopoJson
    {

        public int[][][] Arcs { get; set; } = [[[]]];
        public Point ToPoint(int x, int y)
        {
            return Transform.ToPoint(x, y);
        }
        // ReSharper disable once MemberCanBePrivate.Global
        public Transform Transform { get; set; } = new();
        public Dictionary<string, Layer> Objects { get; set; } = [];
        public MapData? GetLayer(string layerName)
        {
            var layer = Objects.GetValueOrDefault(layerName);
            if (layer is null || Detailer is null)
            {
                return null;
            }
            return new MapData(Detailer, layer);
        }
        public MapData CreateLayer()
        {
            return Detailer is null ? new MapData([], null) : new MapData(Detailer, null);
        }

        public Point[][][]? Detailer { get; set; }
    }
    public class MapData
    {
        private readonly Layer? _layer;
        private readonly Point[][][] _arcs;
        public int Simplify = 0;
        public enum PolygonType : byte
        {
            None = 1,
            Coast = 2,
            Pref = 4,
            Area = 8,
            City = 16
        }
        internal MapData(Point[][][] arcs, Layer? layer)
        {

            _layer = layer;
            _arcs = arcs;
        }
        public void AddVertex(Tess tess, int[] contours)
        {
            List<ContourVertex> result = [];
            foreach (var index in contours)
            {
                var index1 = RealIndex(index);
                var coords = _arcs[Simplify][index1];
                var poi = coords.Select(x => new ContourVertex { Position = new Vec3(x.X, x.Y, 0) });
                result.AddRange(index >= 0 ? poi : poi.Reverse());
            }
            tess.AddContour(result);
        }
        
        public Point[] GetLine(int index)
        {
            var index1 = index >= 0 ? index : -index - 1;
            var coords = _arcs[Simplify][index1];
            return index >= 0 ? [..coords] : [..Enumerable.Reverse(coords)];
        }

        public SKPath ToPath(Feature feature)
        {
            SKPath path = new();
            foreach (var arc in feature.Arcs)
            {
                List<SKPoint> result = [];
                foreach (var index in arc[0])
                {
                    var index1 = RealIndex(index);
                    var coords = _arcs[^1][index1].Select<Point, SKPoint>(x => x).ToList();
                    if (index < 0)
                    {
                        coords.Reverse();
                    }
                    result.AddRange(coords);
                }
                path.AddPoly(result.ToArray());
            }

            return path;
        }
        
        public static int RealIndex(int value)
        {
            return value >= 0 ? value : -value - 1;
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
