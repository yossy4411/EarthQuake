using MessagePack;
using SkiaSharp;

namespace EarthQuake.Core.TopoJson
{
    /// <summary>
    /// 2Dの位置を表す構造体 絶対位置を表す
    /// </summary>
    /// <param name="x">x座標</param>
    /// <param name="y">y座標</param>
    [MessagePackObject]
    public readonly struct Point(float x, float y)
    {
        [Key(0)]
        internal float X { get; init; } = x;
        [Key(1)]
        internal float Y { get; init; } = y;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Point() : this(0, 0) { } 
        
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
        public string[] Names { get; init; } = names;
        [Key(1)]
        public Point[][][] Points { get; init; } = points;
    }
    [MessagePackObject]
    public class CalculatedBorders(string[] names, Point[][][] points, int[][][] indices)
    {
        [Key(0)]
        public string[] Names { get; init; } = names;

        [Key(1)]
        public Point[][][] Points { get; init; } = points;
        
        [Key(2)]
        public int[][][] Indices { get; init; } = indices;
    }
    [MessagePackObject]
    public class PolygonsSet(CalculatedPolygons filling, Dictionary<string, SubPolygon> subPolygons, CalculatedBorders border)
    {
        [Key(0)]
        public CalculatedPolygons Filling { get; init; } = filling;
        [Key(1)]
        public Dictionary<string, SubPolygon> SubPolygons { get; init; } = subPolygons;
		[Key(2)]
        public CalculatedBorders Border { get; init; } = border;
    }
    [MessagePackObject]
    public class Border(Index[] containedIndices, Point[][] points)
    {
        public Border() : this([], [])
        {

        }
        [Key(0)]
        public Point[][] Points { get; init; } = points;
        [Key(1)]
        public Index[] ContainedIndices { get; init; } = containedIndices;
    }
    [MessagePackObject] 
    public class SubPolygon(string[] names, int[][] indices)
    {
        [Key(0)]
        public string[] Names { get; init; } = names;
        [Key(1)]
        public int[][] Indices { get; init; } = indices;
    }
    [MessagePackObject]
    public struct Index(int parentIndex, int childIndex)
    {
        [Key(0)]
        public int ParentIndex = parentIndex;
        [Key(1)]
        public int ChildIndex = childIndex;
    }
    [MessagePackObject]
    public class Property(string name, string? nameKana)
    {
        /// <summary>
        /// 地物の名前
        /// </summary>
        [Key(0)]
        public string Name { get; init; } = name;
        /// <summary>
        /// 地物のひらがな名
        /// </summary>
        [Key(1)]
        public string? NameKana { get; init; } = nameKana;
    }
}
