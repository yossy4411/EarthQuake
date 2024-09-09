using SkiaSharp;
using EarthQuake.Core.EarthQuakes;

namespace EarthQuake.Map.Colors
{
    public class Kiwi3Color
    {
        public static SKColor S1 => new(60, 90, 130);
        public static SKColor S2 => new(30, 130, 230);
        public static SKColor S3 => new(120, 230, 220);
        public static SKColor S4 => new(255, 255, 150);
        public static SKColor S5L => new(255, 210, 0);
        public static SKColor S5H => new(255, 150, 0);
        public static SKColor S6L => new(240, 50, 0);
        public static SKColor S6H => new(190, 0, 0);
        public static SKColor S7 => new(140, 0, 0);
        public static SKColor Unknown => new(40, 70, 110);
        public static SKColor GetColor(Scale scale)
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
