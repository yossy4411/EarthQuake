using QuakeType = EarthQuake.Core.EarthQuakes.P2PQuake.PQuakeData.IssueD.QuakeType;

namespace EarthQuake.Core.EarthQuakes.P2PQuake;

/// <summary>
/// P2P地震情報の変換クラス
/// </summary>
public static class P2PConverter
{
    /// <summary>
    /// 震度を変換
    /// </summary>
    /// <param name="text">P2P地震情報での文字列</param>
    /// <returns>震度</returns>
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

    /// <summary>
    /// 津波を変換
    /// </summary>
    /// <param name="text">P2P地震情報での文字列</param>
    /// <returns>震度</returns>
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

    /// <summary>
    /// 津波を変換
    /// </summary>
    /// <param name="tsunami">津波のデータ</param>
    /// <returns>文字列</returns>
    public static string ToScreenString(this Tsunami tsunami)
    {
        return tsunami switch
        {
            Tsunami.None => "津波の心配なし",
            Tsunami.Unknown => "津波情報不明",
            Tsunami.Checking => "津波情報調査中",
            Tsunami.NonEffective => "若干の海面変動はあるが、被害の心配なし",
            Tsunami.Watch => "津波注意報を発表中",
            Tsunami.Warning => "津波警報または大津波警報を発表中",
            _ => " [エラー] "
        };
    }

    /// <summary>
    /// 地震の種類を変換
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
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