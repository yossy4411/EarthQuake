using EarthQuake.Core.EarthQuakes;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Map.Colors
{
    public class QuarogColor
    {
        public static SKColor S1 => new(50, 90, 140);
        public static SKColor S2 => new(50, 120, 210);
        public static SKColor S3 => new(50, 210, 230);
        public static SKColor S4 => new(250, 250, 140);
        public static SKColor S5L => new(250, 190, 50);
        public static SKColor S5H => new(250, 130, 30);
        public static SKColor S6L => new(230,20,20);
        public static SKColor S6H => new(160,20,50);
        public static SKColor S7 => new(90,20,70);
        public static SKColor Unknown => new(70, 80, 90);
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
                _ => Unknown
            };
        }
    }
}
