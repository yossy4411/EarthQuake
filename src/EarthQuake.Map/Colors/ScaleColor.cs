using EarthQuake.Core.EarthQuakes;
using SkiaSharp;

namespace EarthQuake.Map.Colors;

/// <summary>
/// 震度配色
/// </summary>
public static class ScaleColor
{
    private static SKColor[] DefaultColors =>
    [
        SKColors.LightGray, // 震度なし/不明/その他
        SKColors.DimGray, // 震度1
        SKColors.DeepSkyBlue, // 震度2
        SKColors.GreenYellow, // 震度3
        SKColors.Gold, // 震度4
        SKColors.Orange, // 震度5弱
        SKColors.OrangeRed, // 震度5強
        SKColors.Red, // 震度6弱
        SKColors.Maroon, // 震度6強
        SKColors.Purple, // 震度7
        SKColors.Indigo // 震度8(臨時)/震度7以上
    ];

    private static SKColor[] OriginalColors =>
    [
        new(171, 171, 171), // 震度なし/不明/その他
        new(125, 205, 226), // 震度1
        new(81, 237, 209), // 震度2
        new(140, 237, 80), // 震度3
        new(255, 204, 40), // 震度4
        new(255, 124, 34), // 震度5弱
        new(255, 24, 0), // 震度5強
        new(199, 0, 0), // 震度6弱
        new(206, 12, 131), // 震度6強
        new(114, 24, 125), // 震度7
        new(92, 12, 12) // 震度8(臨時)/震度7以上
    ];

    private static SKColor[] Kiwi3Colors =>
    [
        new(40, 70, 110), // 震度なし/不明/その他
        new(60, 90, 130), // 震度1
        new(30, 130, 230), // 震度2
        new(120, 230, 220), // 震度3
        new(255, 255, 150), // 震度4
        new(255, 210, 0), // 震度5弱
        new(255, 150, 0), // 震度5強
        new(240, 50, 0), // 震度6弱
        new(190, 0, 0), // 震度6強
        new(140, 0, 0) // 震度7
    ];

    private static SKColor[] QuarogColors =>
    [
        new(70, 80, 90), // 震度なし/不明/その他
        new(50, 90, 140), // 震度1
        new(50, 120, 210), // 震度2
        new(50, 210, 230), // 震度3
        new(250, 250, 140), // 震度4
        new(250, 190, 50), // 震度5弱
        new(250, 130, 30), // 震度5強
        new(230, 20, 20), // 震度6弱
        new(160, 20, 50), // 震度6強
        new(90, 20, 70) // 震度7
    ];

    /// <summary>
    /// おかゆグループ独自のカラースキーム (非推奨)
    /// </summary>
    /// <param name="scale"></param>
    /// <returns></returns>
    public static SKColor GetOriginalColor(this Scale scale)
    {
        return scale switch
        {
            Scale.Unknown => OriginalColors[0],
            Scale.Scale1 => OriginalColors[1],
            Scale.Scale2 => OriginalColors[2],
            Scale.Scale3 => OriginalColors[3],
            Scale.Scale4 => OriginalColors[4],
            Scale.Scale5L => OriginalColors[5],
            Scale.Scale5H => OriginalColors[6],
            Scale.Scale6L => OriginalColors[7],
            Scale.Scale6H => OriginalColors[8],
            Scale.Scale7 => OriginalColors[9],
            Scale.Scale8 => OriginalColors[10],
            _ => OriginalColors[0]
        };
    }

    /// <summary>
    /// Kiwi monitor 3カラースキーム
    /// </summary>
    /// <param name="scale"></param>
    /// <returns></returns>
    public static SKColor GetKiwi3Color(this Scale scale)
    {
        return scale switch
        {
            Scale.Unknown => Kiwi3Colors[0],
            Scale.Scale1 => Kiwi3Colors[1],
            Scale.Scale2 => Kiwi3Colors[2],
            Scale.Scale3 => Kiwi3Colors[3],
            Scale.Scale4 => Kiwi3Colors[4],
            Scale.Scale5L => Kiwi3Colors[5],
            Scale.Scale5H => Kiwi3Colors[6],
            Scale.Scale6L => Kiwi3Colors[7],
            Scale.Scale6H => Kiwi3Colors[8],
            Scale.Scale7 => Kiwi3Colors[9],
            _ => Kiwi3Colors[0]
        };
    }

    /// <summary>
    /// Quarogカラースキーム
    /// </summary>
    /// <param name="scale"></param>
    /// <returns></returns>
    public static SKColor GetQuarogColor(this Scale scale)
    {
        return scale switch
        {
            Scale.Unknown => QuarogColors[0],
            Scale.Scale1 => QuarogColors[1],
            Scale.Scale2 => QuarogColors[2],
            Scale.Scale3 => QuarogColors[3],
            Scale.Scale4 => QuarogColors[4],
            Scale.Scale5L => QuarogColors[5],
            Scale.Scale5H => QuarogColors[6],
            Scale.Scale6L => QuarogColors[7],
            Scale.Scale6H => QuarogColors[8],
            Scale.Scale7 => QuarogColors[9],
            _ => QuarogColors[0]
        };
    }

    public static SKColor GetDefaultColor(this Scale scale)
    {
        return scale switch
        {
            Scale.Unknown => DefaultColors[0],
            Scale.Scale1 => DefaultColors[1],
            Scale.Scale2 => DefaultColors[2],
            Scale.Scale3 => DefaultColors[3],
            Scale.Scale4 => DefaultColors[4],
            Scale.Scale5L => DefaultColors[5],
            Scale.Scale5H => DefaultColors[6],
            Scale.Scale6L => DefaultColors[7],
            Scale.Scale6H => DefaultColors[8],
            Scale.Scale7 => DefaultColors[9],
            Scale.Scale8 => DefaultColors[10],
            _ => DefaultColors[0]
        };
    }

    public static SKColor GetColor(string tag, Scale scale)
    {
        return tag switch
        {
            "Original" or "original" or "o" => scale.GetOriginalColor(),
            "Kiwi3" or "kiwi3" or "k" => scale.GetKiwi3Color(),
            "Quarog" or "quarog" or "q" => scale.GetQuarogColor(),
            _ => scale.GetDefaultColor()
        };
    }
}