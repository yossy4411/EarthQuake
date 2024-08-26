using MessagePack;
using SkiaSharp;

namespace EarthQuake.Core.TopoJson;

/// <summary>
/// 2Dの位置を表す構造体 絶対位置を表す
/// </summary>
/// <param name="x">x座標</param>
/// <param name="y">y座標</param>
[MessagePackObject]
public readonly struct Point(float x, float y)
{
    [Key(0)]
    public float X { get; } = x;
    [Key(1)]
    public float Y { get; } = y;
    
        
    public static implicit operator SKPoint(Point p) => new(p.X, p.Y);
        

    public static float Distance(Point p1, Point p2)
    {
        var dx = p1.X - p2.X;
        var dy = p1.Y - p2.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
    public void Deconstruct(out float x, out float y)
    {
        x = X;
        y = Y;
    }
}
    
[MessagePackObject]
public class CalculatedPolygons(string[] names, Point[][][] points)
{
    [Key(0)]
    public string[] Names { get; } = names;
    [Key(1)]
    public Point[][][] Points { get; } = points;
}

[MessagePackObject]
public class CalculatedBorders(string[] names, Point[][][] points, int[][][] indices)
{
    [Key(0)]
    public string[] Names { get; } = names;

    [Key(1)]
    public Point[][][] Points { get; } = points;
        
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
public struct Index(int parentIndex, int childIndex)
{
    [Key(0)]
    public int ParentIndex = parentIndex;
    [Key(1)]
    public int ChildIndex = childIndex;
}