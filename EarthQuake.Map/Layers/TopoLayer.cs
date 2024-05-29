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
    public abstract class TopoLayer(TopoJson? json, string? layerName) : MapLayer
    {
        public MapData? Data = json is null ? null : layerName is null ? json.CreateLayer() : json.GetLayer(layerName);
        public override void Update(GeomTransform geo)
        {
            base.Update(geo);
            Data = null; // "json"への参照を切っておく
        }
        /// <summary>
        /// ポリゴンとその表示範囲を保存するためのレコード
        /// </summary>
        /// <param name="Vertices">ポリゴン</param>
        /// <param name="Rect">表示範囲</param>
        internal protected record Polygon(SKVertices Vertices, SKRect Rect);
    }
}
