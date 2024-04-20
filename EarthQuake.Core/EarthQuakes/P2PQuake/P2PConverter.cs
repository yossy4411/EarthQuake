using QuakeType = EarthQuake.Core.EarthQuakes.P2PQuake.PQuakeData.IssueD.QuakeType;

namespace EarthQuake.Core.EarthQuakes.P2PQuake
{
    internal static class P2PConverter
    {
        public static Scale ParseScale(string text)
        {
            return text switch
            {
                "1" => Scale.Scale1,
                "2" => Scale.Scale2,
                "3" => Scale.Scale3,
                "4" => Scale.Scale4,
                "5弱" => Scale.Scale5L,
                "5弱以上（推定）" => Scale.Scale5U,
                "5強" => Scale.Scale5H,
                "6弱" => Scale.Scale6L,
                "6強" => Scale.Scale6H,
                "7" => Scale.Scale7,
                "8" => Scale.Scale8,
                _ => Scale.Unknown,
            };
        }
        public static Tsunami ParseTsunami(string text)
        {
            return text switch
            {
                "0" => Tsunami.None,
                "1" => Tsunami.Warning,
                "2" => Tsunami.Checking,
                "3" => Tsunami.Unknown,
                _ => Tsunami.Unknown,
            };
        }
        public static string ToScreenString(this QuakeType type)
        {
            return type switch
            {
                QuakeType.ScalePrompt => "震度速報",
                QuakeType.DetailScale => "各地の震度の情報",
                QuakeType.ScaleAndDestination => "震源・震度の情報",
                QuakeType.Destination => "震源情報",
                QuakeType.Foreign => "遠地地震の情報",
                QuakeType.Other => "その他の情報",
                _ => " [エラー] "
            };
        }
    }
}