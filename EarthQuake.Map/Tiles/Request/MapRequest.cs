namespace EarthQuake.Map.Tiles.Request;

public abstract class MapRequest
{
    public MapRequestCallback? Finished { get; set; }
}

public delegate void MapRequestCallback(MapRequest request, object? result);
