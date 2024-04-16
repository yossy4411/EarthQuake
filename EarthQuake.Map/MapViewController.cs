using EarthQuake.Core;
using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Core.TopoJson;
using EarthQuake.Map.Layers.OverLays;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static EarthQuake.Core.TopoJson.MapData;

namespace EarthQuake.Map
{
    public enum LayerType
    {
        PrefOnly
    }
    public class MapViewController
    {
        public GeoTransform Geo { get; set; } = new();
        private MapLayer[] _mapLayers = [];
        public float Rotation { get; set; } = 0f;
        public MapLayer[] MapLayers
        {
            get { return _mapLayers; }
            set
            {
                _mapLayers = value; 
                foreach (var item in _mapLayers)
                {
                    item.Update(Geo);
                }
            }
        }
        public MapViewController(TopoJson json, GeoTransform geo, PolygonType[]? types = null)
        {
            Geo = geo;
            if (types == null)
            {
                PolygonType[] geom = CalculateTypes(json);
                Geo.GeometryType = geom;
            }
            else
            {
                Geo.GeometryType = types;
            }

            
        }

        public static PolygonType[] CalculateTypes(TopoJson json)
        {
            var rep = new int[json.Arcs.Length];
            foreach (var geo in json.Objects["eew"].Geometries)
            {
                foreach (var a in geo.Arcs)
                {
                    foreach (var b in a)
                    {
                        foreach (var c in b)
                            rep[c >= 0 ? c : -c - 1]++;
                    }
                }
            }
            var geom = rep.Select(v =>
            {
                if (v == 1) return PolygonType.Coast;
                if (v >= 2) return PolygonType.Pref;
                return PolygonType.None;
            }).ToArray();

            SetPolygonTypes("info", PolygonType.Area, json, geom);
            SetPolygonTypes("city", PolygonType.City, json, geom);
            return geom;
        }

        private static void SetPolygonTypes(string t, PolygonType replace, TopoJson json, PolygonType[] geom)
        {
            foreach (var c in from geo in json.Objects[t].Geometries
                              from a in geo.Arcs
                              from b in a
                              from c in b
                              where geom[c >= 0 ? c : -c - 1] == PolygonType.None
                              select c)
            {
                geom[c >= 0 ? c : -c - 1] = replace;
            }
        }

        public void RenderBase(SKCanvas canvas, float scale, SKRect bounds)
        {
            foreach (var layer in MapLayers)
            {
                if (layer is not ForeGroundLayer)
                {
                    layer.Render(canvas, scale, bounds);
                }
            }

        }
        public void RenderForeGround(SKCanvas canvas, float scale, SKRect bounds, object? param = null)
        {
            foreach (var layer in MapLayers)
            {
                if (layer is ForeGroundLayer fore)
                {
                    if (fore is Hypo3DViewLayer hypo)
                        hypo.Render(canvas, param as SKRect? ?? SKRect.Empty);
                    else
                        fore.Render(canvas, scale, bounds);
                }
                    
            }
            
        }
    }
}
