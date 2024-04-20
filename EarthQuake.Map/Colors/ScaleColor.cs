using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EarthQuake.Core.TopoJson;
using EarthQuake.Core.EarthQuakes;

namespace EarthQuake.Map.Colors
{
    public abstract class ScaleColor
    {
        
        public static SKColor S0 => SKColors.LightGray;
        public static SKColor S1 => SKColors.DimGray;
        public static SKColor S2 => SKColors.DeepSkyBlue;
        public static SKColor S3 => SKColors.GreenYellow;
        public static SKColor S4 => SKColors.Gold;
        public static SKColor S5L => SKColors.Orange;
        public static SKColor S5H => SKColors.OrangeRed;
        public static SKColor S6L => SKColors.Maroon;
        public static SKColor S6H => SKColors.DeepPink;
        public static SKColor S7 => SKColors.Purple;
        public static SKColor S8 => SKColors.Indigo;
        public static SKColor GetFromP2P(Scale scale)
        {
            return scale switch
            {
                Scale.Scale1 => S1,
                Scale.Scale2 => S2,
                Scale.Scale3 => S3,
                Scale.Scale4 => S4,
                Scale.Scale5L => S5L,
                Scale.Scale5H => S5H,
                Scale.Scale6L => S6L,
                Scale.Scale6H => S6H,
                Scale.Scale7 => S7,
                _ => S8
            };
        }
    }
}
