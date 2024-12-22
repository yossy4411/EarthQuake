using EarthQuake.Core;
using LibTessDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;

// ReSharper disable ClassNeverInstantiated.Global


namespace MapDataGenerator
{
    public class TopoJson
    {

        public int[][][] Arcs { get; set; } = [[[]]];
        public SKPoint ToPoint(int x, int y)
        {
            return Transform.ToPoint(x, y);
        }

        public Transform Transform { get; set; } = new();
        public Dictionary<string, Layer> Objects { get; set; } = [];
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
                var array = JArray.Load(reader);
                return array.First?.First?.Type == JTokenType.Array ? array.ToObject<int[][][]>() : [array.ToObject<int[][]>() ?? [[]]];
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
