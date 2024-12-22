namespace EarthQuake.Core.EarthQuakes;

/// <summary>
/// 震度
/// 気象庁の震度階級と緊急地震速報での震度階級
/// </summary>
public enum Scale
{
    Unknown = -1, // 不明
    Scale1 = 10, // 震度１
    Scale2 = 20, // 震度２
    Scale3 = 30, // 震度３
    Scale4 = 40, // 震度４
    Scale5L = 45, // 震度５弱
    Scale5U = 46, // 震度５弱以上（推定）
    Scale5H = 50, // 震度５強
    Scale6L = 55, // 震度６弱
    Scale6H = 60, // 震度６強
    Scale7 = 70, // 震度７
    Scale8 = 80 // 震度７以上／震度８（臨時） - 緊急地震速報でのみ使用
}

public static class ScaleConverter
{
    /// <summary>
    /// 震度を画面表示用の文字列に変換
    /// </summary>
    /// <param name="scale">元の震度</param>
    /// <returns>文字列</returns>
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

    /// <summary>
    /// 震度を整数の値に変換
    /// </summary>
    /// <param name="scale">元の震度</param>
    /// <returns>整数での震度</returns>
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

    /// <summary>
    /// 一般的な震度文字列からScaleに変換
    /// </summary>
    /// <param name="formatedText">文字列</param>
    /// <returns>震度</returns>
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
            "8" => Scale.Scale8,
            _ => Scale.Unknown,
        };
}