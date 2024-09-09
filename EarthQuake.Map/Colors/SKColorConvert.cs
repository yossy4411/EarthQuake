using SkiaSharp;

namespace EarthQuake.Map.Colors
{
    public static class ColorBrush
    {
        public static SKColor IncreaseBrightness(this SKColor color, int percentage)
        {
            // RGB成分を10%増加させる
            var r = (int)(color.Red * (1 + percentage / 100f));
            var g = (int)(color.Green * (1 + percentage / 100f));
            var b = (int)(color.Blue * (1 + percentage / 100f));

            // 255を超えないように制限する
            r = Math.Min(r, 255);
            g = Math.Min(g, 255);
            b = Math.Min(b, 255);

            return new SKColor((byte)r, (byte)g, (byte)b, color.Alpha);
        }
    }
}
