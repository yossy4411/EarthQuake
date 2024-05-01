namespace EarthQuake.Core.EarthQuakes
{
    public enum Scale
    {
        Unknown = -1,
        Scale1 = 10,
        Scale2 = 20,
        Scale3 = 30,
        Scale4 = 40,
        Scale5L = 45,
        Scale5U = 46,
        Scale5H = 50,
        Scale6L = 55,
        Scale6H = 60,
        Scale7 = 70,
        Scale8 = 80
    }
    public static class Converter
    {
        public static string ToScreenString(this Scale scale) => 
            scale switch
            {
                Scale.Scale1 => "震度１",
                Scale.Scale2 => "震度２",
                Scale.Scale3 => "震度３",
                Scale.Scale4 => "震度４",
                Scale.Scale5L => "震度５弱",
                Scale.Scale5U => "震度５弱以上（推定）",
                Scale.Scale5H => "震度５強",
                Scale.Scale6L => "震度６弱",
                Scale.Scale6H => "震度６強",
                Scale.Scale7 => "震度７",
                Scale.Scale8 => "震度８（臨時）",
                Scale.Unknown => "不明",
                _ => " [エラー] ",
            };
        public static int ToInt(this Scale scale) =>
            scale switch
            {
                Scale.Scale1 => 1,
                Scale.Scale2 => 2,
                Scale.Scale3 => 3,
                Scale.Scale4 => 4,
                Scale.Scale5L => 5,
                Scale.Scale5U => 6,
                Scale.Scale5H => 7,
                Scale.Scale6L => 8,
                Scale.Scale6H => 9,
                Scale.Scale7 => 10,
                Scale.Scale8 => 11,
                Scale.Unknown => 0,
                _ => 0
            };
        public static Scale FromInt(int jmaScale) =>
            jmaScale switch
            {
                1 => Scale.Scale1,
                2 => Scale.Scale2,
                3 => Scale.Scale3,
                4 => Scale.Scale4,
                5 => Scale.Scale5L,
                6 => Scale.Scale5U,
                7 => Scale.Scale5H,
                8 => Scale.Scale6L,
                9 => Scale.Scale6H,
                10 => Scale.Scale7,
                11 => Scale.Scale8,
                0 => Scale.Unknown,
                _ => Scale.Unknown
            };
        public static Scale FromString(string formatedText) =>
            formatedText switch
            {
                "1" => Scale.Scale1,
                "2" => Scale.Scale2,
                "3" => Scale.Scale3,
                "4" => Scale.Scale4,
                "5弱" => Scale.Scale5L,
                "5強" => Scale.Scale5H,
                "6弱" => Scale.Scale6L,
                "6強" => Scale.Scale6H,
                "7" => Scale.Scale7,
                _ => Scale.Unknown,
            };
    }
}
