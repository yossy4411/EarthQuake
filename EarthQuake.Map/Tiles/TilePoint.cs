namespace EarthQuake.Map.Tiles;

/// <summary>
/// タイル上の点
/// </summary>
/// <param name="X">X座標</param>
/// <param name="Y">Y座標</param>
/// <param name="Z">Z座標</param>
public readonly record struct TilePoint(int X, int Y, int Z)
{
    public TilePoint Add(int x, int y) => new(X + x, Y + y, Z);
    public static TilePoint operator +(TilePoint point, (int x, int y) point1) => point.Add(point1.x, point1.y);

    public bool Equals(TilePoint other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }
}