using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using SkiaSharp;

namespace EarthQuake.Core.TopoJson;

    
[MessagePackObject]
public class CalculatedPolygons(string[] names, SKPoint[][][] points)
{
    [Key(0)]
    public string[] Names { get; } = names;
    [Key(1)]
    public SKPoint[][][] Points { get; } = points;
}

[MessagePackObject]
public class CalculatedBorders(string[] names, SKPoint[][][] points, int[][][] indices)
{
    [Key(0)]
    public string[] Names { get; } = names;

    [Key(1)]
    public SKPoint[][][] Points { get; } = points;
        
    [Key(2)]
    public int[][][] Indices { get; } = indices;
}

[MessagePackObject]
public class PolygonsSet(CalculatedPolygons filling, Dictionary<string, SubPolygon> subPolygons, CalculatedBorders border)
{
    [Key(0)]
    public CalculatedPolygons Filling { get; } = filling;
    [Key(1)]
    public Dictionary<string, SubPolygon> SubPolygons { get; } = subPolygons;
    [Key(2)]
    public CalculatedBorders Border { get; } = border;
}

[MessagePackObject] 
public class SubPolygon(string[] names, int[][] indices)
{
    [Key(0)]
    public string[] Names { get; } = names;
    [Key(1)]
    public int[][] Indices { get; } = indices;
}

[MessagePackObject]
public class WorldPolygonSet(SKPoint[]? polygons/*, SKPoint[][] borders*/)
{
    [Key(0)]
    public SKPoint[]? Polygons { get; } = polygons;
}

public static class Serializer
{
    private class SkPointFormatter : IMessagePackFormatter<SKPoint>
    {
        public void Serialize(ref MessagePackWriter writer, SKPoint value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        public SKPoint Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var count = reader.ReadArrayHeader();
            if (count != 2) throw new MessagePackSerializationException("Invalid SKPoint format");
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            return new SKPoint(x, y);
        }
    }

    public class SkiaSharpResolver : IFormatterResolver
    {
        private static readonly SkPointFormatter SkPointFormatter = new();

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            static FormatterCache()
            {
                Formatter = typeof(T) == typeof(SKPoint) ? (IMessagePackFormatter<T>)SkPointFormatter : null;
            }
        }


        

        
    }
    private static readonly IFormatterResolver Resolver = 
        CompositeResolver.Create(new SkiaSharpResolver(), StandardResolver.Instance);
     
    private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard
         .WithCompression(MessagePackCompression.Lz4BlockArray).WithResolver(Resolver);

    public static byte[] Serialize<T>(T obj)
    {
        return MessagePackSerializer.Serialize(obj, Options);
    }

    public static T Deserialize<T>(byte[] bytes)
    {
        return MessagePackSerializer.Deserialize<T>(bytes, Options);
    }

    public static T Deserialize<T>(Stream stream)
    {
        return MessagePackSerializer.Deserialize<T>(stream, Options);
    }
}