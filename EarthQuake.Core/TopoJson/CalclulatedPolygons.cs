using MessagePack;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Core.TopoJson
{
    [MessagePackObject]
    public readonly struct Point(float x, float y)
    {
        [Key(0)]
        private float X { get; init; } = x;
        [Key(1)]
        private float Y { get; init; } = y;

        public static implicit operator SKPoint(Point p) => new(p.X, p.Y);
    }
    [MessagePackObject]
    public class Polygon(Point[][] points, float mix, float max, float miy, float may)
    {
        [Key(0)]
        public Point[][] Points { get; init; } = points;
        [Key(1)]
        public float MinX { get; init; } = mix;
        [Key(2)]
        public float MaxX { get; init; } = max;
        [Key(3)]
        public float MinY { get; init; } = miy;
        [Key(4)]
        public float MaxY { get; init; } = may;
    }
    [MessagePackObject]
    public class CalculatedPolygons(string[] names, Polygon[] points)
    {
        [Key(0)]
        public string[] Names { get; init; } = names;
        [Key(1)]
        public Polygon[] Points { get; init; } = points;
    }
    [MessagePackObject]
    public class CalculatedBorders(string[] names, Border[] points)
    {
        [Key(0)]
        public string[] Names { get; init; } = names;
        [Key(1)]
        public Border[] Points { get; init; } = points;
    }
    [MessagePackObject]
    public class PolygonsSet(CalculatedPolygons info, CalculatedPolygons city, CalculatedBorders border)
    {
        [Key("info")]
        public CalculatedPolygons Info {  get; init; } = info;
        [Key("city")]
        public CalculatedPolygons City { get; init; } = city;
        [Key("Border")]
        public CalculatedBorders Border { get; init; } = border;
    }
    [MessagePackObject]
    public class Border(Index[] containedIndice, Point[][] points)
    {
        public Border() : this([], [])
        {

        }
        [Key(0)]
        public Point[][] Points { get; init; } = points;
        [Key(1)]
        public Index[] ContainedIndice { get; init; } = containedIndice;
    }
    [MessagePackObject]
    public struct Index(int parentIndex, int childIndex)
    {
        [Key(0)]
        public int ParentIndex = parentIndex;
        [Key(1)]
        public int ChildIndex = childIndex;
    }
}
