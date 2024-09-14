using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using SkiaSharp;

namespace EarthQuake.Core.TopoJson;


[MessagePackObject]
public class PolygonFeatures(string[] names, int[][][] indices)
{
    [Key(0)]
    public string[] Names { get; } = names;
        
    [Key(1)]
    public int[][][] Indices { get; } = indices;
}

[MessagePackObject]
public class PolygonsSet(Dictionary<string, PolygonFeatures> filling, PointsSet points)
{
    [Key(0)]
    public Dictionary<string, PolygonFeatures> Filling { get; } = filling;
    
    [Key(1)]
    public PointsSet Points { get; } = points;
}

[MessagePackObject] 
public readonly struct IntPoint(int x, int y)
{
    [Key(0)]
    public int X { get; } = x;

    [Key(1)]
    public int Y { get; } = y;
    
    public static IntPoint operator +(IntPoint a, IntPoint b) => new(a.X + b.X, a.Y + b.Y);
    
    public static IntPoint operator -(IntPoint a, IntPoint b) => new(a.X - b.X, a.Y - b.Y);
    
    public static IntPoint operator *(IntPoint a, int b) => new(a.X * b, a.Y * b);
    
    public static SKPoint operator *(IntPoint a, float b) => new(a.X * b, a.Y * b);
    
    public static SKPoint operator *(IntPoint a, SKPoint b) => new(a.X * b.X, a.Y * b.Y);
    
    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }
    
    public static implicit operator SKPoint(IntPoint point) => new(point.X, point.Y);
}

[MessagePackObject]
public class PointsSet(IntPoint[][][] points, Transform transform)
{
    [Key(0)]
    public IntPoint[][][] Points { get; } = points;
    
    [Key(1)]
    public Transform Transform { get; } = transform;
}

[MessagePackObject]
public class Transform(SKPoint scale, SKPoint translate)
{
    [Key(0)]
    public SKPoint Scale { get; } = scale;
    
    [Key(1)]
    public SKPoint Translate { get; } = translate;
    public SKPoint ToPoint(int x, int y)
    {
        return new SKPoint(x * Scale.X + Translate.X, y * Scale.Y + Translate.Y);
    }
    public SKPoint ToPoint(IntPoint point)
    {
        return new SKPoint(point.X * Scale.X + Translate.X, point.Y * Scale.Y + Translate.Y);
    }
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