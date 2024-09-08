using EarthQuake.Core.TopoJson;
using LibTessDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Core.GeoJson
{
    public class GeoJson
    {
        public Feature[] Features { get; set; } = [];
        public class Feature
        {
            public GeometryF? Geometry { get; set; } = new();
            public class GeometryF
            {
                [JsonConverter(typeof(ArcConverter))]
                public double[][][][] Coordinates { get; set; } = [];
                private class ArcConverter : JsonConverter
                {
                    public override bool CanConvert(Type objectType)
                    {
                        return objectType == typeof(double[][][]) || objectType == typeof(double[][][][]);
                    }

                    public override double[][][][]? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
                    {
                        var array = JArray.Load(reader);
                        return array.First?.First?.First?.Type == JTokenType.Array ? array.ToObject<double[][][][]>() : [array.ToObject<double[][][]>() ?? [[[]]]];
                    }

                    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
                    {
                        throw new NotImplementedException();
                    }
                }
                public void AddVertex(Tess tess, GeomTransform geo, int vertex, ref float minX, ref float minY, ref float maxX, ref float maxY)
                {

                    var coordinates = Coordinates[vertex];
                    List<ContourVertex> result = [];
                    foreach (var coord in coordinates[0])
                    {
                        var point = geo.Translate(coord[0], coord[1]);
                        minX = Math.Min(minX, point.X);
                        maxX = Math.Max(maxX, point.X);
                        minY = Math.Min(minY, point.Y);
                        maxY = Math.Max(maxY, point.Y);

                        result.Add(new ContourVertex() { Position = new Vec3(point.X, point.Y, 0) });
                    }
                    tess.AddContour(result);
                }
            }
        }
    }
}
