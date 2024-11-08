using System.Drawing;
using SkiaSharp;
using VectorTiles.Mvt;
using VectorTiles.Styles;
using VectorTiles.Values;

namespace EarthQuake.Map.Tiles.Vector;

public static class VectorMapSKParser
{
    public static VectorTileFeature[] ParsePaths(this VectorMapStyle style, Stream stream, TilePoint point)
    {
        var tileFeatures = new List<VectorTileFeature>();
        var (x, y, z) = point;
        var tile = MapboxTileReader.Read(stream, z, x, y, style);
        foreach (var tileLayer in tile.Layers)
        {
            foreach (var tileLayerFeature in tileLayer.Features)
            {
                tileLayerFeature.Tags["$zoom"] = new ConstFloatValue(z);
                var type = tileLayerFeature.Type switch
                {
                    MapboxTile.Layer.Feature.FeatureType.Point => "Point",
                    MapboxTile.Layer.Feature.FeatureType.LineString => "LineString",
                    MapboxTile.Layer.Feature.FeatureType.Polygon => "Polygon",
                    _ => "Unknown"
                };
                tileLayerFeature.Tags["$type"] = new ConstStringValue(type);
            }
        }
        foreach (var styleLayer in style.Layers)
        {
            var layer = tile.Layers.FirstOrDefault(l => l.Name == styleLayer.Source);
            if (layer is null) continue;
            foreach (var feature in layer.Features.Where(f => styleLayer.IsVisible(f.Tags)))
            {
                switch (styleLayer)
                {
                    case VectorFillStyleLayer fillLayer:
                        tileFeatures.Add(new VectorFillFeature(feature, fillLayer, feature.Tags));
                        break;
                    case VectorLineStyleLayer lineLayer:
                        tileFeatures.Add(new VectorLineFeature(feature, lineLayer, feature.Tags));
                        break;
                    case VectorSymbolStyleLayer symbolLayer:
                        tileFeatures.Add(new VectorSymbolFeature(feature, symbolLayer, feature.Tags));
                        break;
                }
            }

        }

        return tileFeatures.ToArray();
    }
    
    public static SKColor ToSKColor(this Color color) => new(color.R, color.G, color.B, color.A);
}