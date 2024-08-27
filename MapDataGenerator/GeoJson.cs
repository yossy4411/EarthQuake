using LibTessDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace MapDataGenerator;

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
            public void AddVertex(Tess tess, int vertex)
            {

                var coordinates = Coordinates[vertex];
                var result = coordinates[0].Select(x => x).Select(point => new ContourVertex { Position = new Vec3((float) point[0], (float) point[1], 0) }).ToList();
                tess.AddContour(result);
            }
        }
    }
}