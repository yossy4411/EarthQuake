using Avalonia.Input;
using EarthQuake.Core;
using EarthQuake.Core.TopoJson;
using SkiaSharp;
using System.Drawing;
namespace EarthQuake.Map
{

    public abstract class MapLayer
    {
        public bool Initialized = false;
        internal abstract void Render(SKCanvas canvas, float scale, SKRect bounds);
        private protected abstract void Initialize(GeoTransform geo);
        public virtual void Update(GeoTransform geo)
        {
            if (!Initialized)
            {
                Initialize(geo);
                Initialized = true;
            }
        }
    }
}
