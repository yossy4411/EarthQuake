using SkiaSharp;

namespace EarthQuake.Map.Tiles.Request;

public abstract class FileTileRequest : MapRequest
{
    public abstract SKObject GetAndParse();
}