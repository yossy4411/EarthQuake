namespace EarthQuake.Map.Tiles.Request;

public abstract class MapRequest
{
    public MapRequestCallback? Finished { get; init; }
}

public delegate void MapRequestCallback(MapRequest request, object? result);
