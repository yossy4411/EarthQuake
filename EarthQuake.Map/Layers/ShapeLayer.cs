using EarthQuake.Core;
using EarthQuake.Core.TopoJson;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Map.Layers
{
    public abstract class ShapeLayer(TopoJson? json, string? layerName) : MapLayer
    {
        public MapData? Data = json is null ? null : layerName is null ? json.CreateLayer() : json.GetLayer(layerName);
        public override void Update(GeoTransform geo)
        {
            base.Update(geo);
            Data = null; // "json"への参照を切っておく
        }
        internal protected record Polygon(SKVertices Vertices, SKRect Rect); // ポリゴンと表示範囲を保存するためのレコード
    }
}
