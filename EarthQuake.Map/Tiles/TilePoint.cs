namespace EarthQuake.Map.Tiles;

public readonly record struct TilePoint(int X, int Y, int Z)
{
    public static readonly TilePoint Empty = new();
    public TilePoint Add(int x, int y) => new(X + x, Y + y, Z);
    public static TilePoint operator +(TilePoint point, (int x, int y)point1) => point.Add(point1.x, point1.y);
}