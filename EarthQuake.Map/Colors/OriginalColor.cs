using EarthQuake.Core.EarthQuakes.P2PQuake;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Map.Colors
{
    public class OriginalColor
    {

        public static SKColor S0 => new(171, 171, 171);
        public static SKColor S1 => new(125, 205, 226);
        public static SKColor S2 => new(81, 237, 209);
        public static SKColor S3 => new(140, 237, 80);
        public static SKColor S4 => new(255, 204, 40);
        public static SKColor S5L => new(255, 124, 34);
        public static SKColor S5H => new(255, 24, 0);
        public static SKColor S6L => new(199, 0, 0);
        public static SKColor S6H => new(206, 12, 131);
        public static SKColor S7 => new(114, 24, 125);
        public static SKColor S8 => new(92, 12, 12);
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
