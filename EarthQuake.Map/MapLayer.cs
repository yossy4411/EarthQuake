using Avalonia.Input;
using Avalonia.Platform;
using EarthQuake.Core;
using EarthQuake.Core.TopoJson;
using SkiaSharp;
using System.Drawing;
namespace EarthQuake.Map
{

    public abstract class MapLayer
    {
        public bool Initialized = false;
        public static readonly SKTypeface Font = SKTypeface.FromStream(AssetLoader.Open(new Uri("avares://EarthQuake/Assets/Fonts/NotoSansJP-Medium.ttf")));
        internal abstract void Render(SKCanvas canvas, float scale, SKRect bounds);
        private protected abstract void Initialize(GeomTransform geo);
        public virtual void Update(GeomTransform geo)
        {
            if (!Initialized)
            {
                Initialize(geo);
                Initialized = true;
            }
        }
    }
}
