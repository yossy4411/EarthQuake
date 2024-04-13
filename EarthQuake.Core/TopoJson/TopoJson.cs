using LibTessDotNet;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

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
        public MapData GetLayer(string layerName)
        {
            return new MapData(Arcs, Objects[layerName], Transform);
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
        public void AddVertex(Tess tess, int[] contours, GeoTransform geo, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            List<ContourVertex> result = [];
            for (int j = 0; j < contours.Length; j++)
            {

                var index = contours[j];
                int _index = index >= 0 ? index : -index - 1;

                int[][] coords = _arcs[_index];
                int x = coords[0][0], y = coords[0][1];
                var _point = geo.Translate(_transform.ToPoint(x, y));
                minX = Math.Min(minX, _point.X);
                maxX = Math.Max(maxX, _point.X);
                minY = Math.Min(minY, _point.Y);
                maxY = Math.Max(maxY, _point.Y);
                List<ContourVertex> vertices = [new ContourVertex() { Position = new Vec3(_point.X, _point.Y, 0) }];
                for (int i = 1; i < coords.Length; i++)
                {
                    x += coords[i][0];
                    y += coords[i][1];
                    
                    var point = geo.Translate(_transform.ToPoint(x, y));
                    minX = Math.Min(minX, point.X);
                    maxX = Math.Max(maxX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxY = Math.Max(maxY, point.Y);
                    if (Simplify == 0 || (_point - point).Length >= Simplify | i == coords.Length - 1)
                    {
                        vertices.Add(new ContourVertex() { Position = new Vec3(point.X, point.Y, 0) });
                        _point = point;
                    }
                }
                if (index >= 0)
                {
                    result.AddRange(vertices);
                }
                else
                {
                    vertices.Reverse();
                    result.AddRange(vertices);
                }

            }
            tess.AddContour(result);
        }
        public void AddLine(SKPath result, int[] contours, GeoTransform geo)
        {
            foreach (var index in contours)
            {
                int _index = index >= 0 ? index : -index - 1;
                int[][] coords = _arcs[_index];
                int x = coords[0][0], y = coords[0][1];
                var _point = geo.Translate(_transform.ToPoint(x, y));

                result.MoveTo(_point);

                for (int i = 1; i < coords.Length; i++)
                {
                    x += coords[i][0];
                    y += coords[i][1];
                    var point = geo.Translate(_transform.ToPoint(x, y));
                    if ((_point - point).Length >= Simplify | i == coords.Length - 1)
                    {
                        result.LineTo(point);
                        _point = point;
                    }
                }



            }

        }
        public (SKPath, SKRect)[][] AddAllLine(GeoTransform geo, PolygonType[]? ocean = null, bool requireCity = true)
        {
            List<(SKPath, SKRect)> coastL = [];
            List<(SKPath, SKRect)> prefL = [];
            List<(SKPath, SKRect)> areaL = [];
            List<(SKPath, SKRect)> cityL = [];

            for (int j = 0; j < _arcs.Length; j++)
            {
                var type = ocean?[j];
                if (type is PolygonType.None || (type is PolygonType.City && !requireCity))
                {
                    continue;
                }
                var coords = _arcs[j];
                int x = coords[0][0], y = coords[0][1];
                var _point = geo.Translate(_transform.ToPoint(x, y));
                float maxX = float.MinValue;
                float maxY = float.MinValue;
                float minX = float.MaxValue;
                float minY = float.MaxValue;
                SKPath path = new();
                path.MoveTo(_point);
                maxX = Math.Max(maxX, _point.X);
                maxY = Math.Max(maxY, _point.Y);
                minX = Math.Min(minX, _point.X);
                minY = Math.Min(minY, _point.Y);
                for (int i = 1; i < coords.Length; i++)
                {
                    x += coords[i][0];
                    y += coords[i][1];
                    var point = geo.Translate(_transform.ToPoint(x, y));
                    if (Simplify == 0 || (_point - point).Length >= Simplify || i == coords.Length - 1)
                    {
                        path.LineTo(point);
                        maxX = Math.Max(maxX, point.X);
                        maxY = Math.Max(maxY, point.Y);
                        minX = Math.Min(minX, point.X);
                        minY = Math.Min(minY, point.Y);
                        _point = point;
                    }
                }
                SKRect rect = new(minX, minY, maxX, maxY);
                switch (type)
                {
                    case PolygonType.City: cityL.Add((path, rect)); break;
                    case PolygonType.Coast: coastL.Add((path, rect)); break;
                    case PolygonType.Area: areaL.Add((path, rect)); break;
                    case PolygonType.Pref: prefL.Add((path, rect)); break;
                }

            }
            return [[.. coastL], [.. prefL], [.. areaL], [.. cityL]];
        }


        public string LayerName => _layer?.Name ?? string.Empty;
        public Feature[]? Geometries => _layer?.Geometries;
    }
    public class Transform
    {
        public double[] Scale { get; set; } = [0, 0];
        public double[] Translate { get; set; } = [0, 0];
        public SKPoint ToPoint(int x, int y)
        {
            return new SKPoint((float)(x * Scale[0] + Translate[0]), (float)(y * Scale[1] + Translate[1]));
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
                JArray array = JArray.Load(reader);
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
