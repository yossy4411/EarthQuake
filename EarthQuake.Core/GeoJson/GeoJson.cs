using EarthQuake.Core.TopoJson;
using LibTessDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EarthQuake.Core.GeoJson;

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
            public void AddVertex(Tess tess, GeomTransform geo, int vertex)
            {

                var coordinates = Coordinates[vertex];
                List<ContourVertex> result = [];
                result.AddRange(coordinates[0].Select(coord => geo.Translate(coord[0], coord[1])).Select(point => new ContourVertex { Position = new Vec3(point.X, point.Y, 0) }));
                tess.AddContour(result);
            }
        }
    }
}