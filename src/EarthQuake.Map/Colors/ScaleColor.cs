using EarthQuake.Core.EarthQuakes;
using SkiaSharp;

namespace EarthQuake.Map.Colors;

/// <summary>
/// 震度配色
/// </summary>
public static class ScaleColor
{
    private static SKColor[] PrimaryColors =>
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

    /// <summary>
    /// Kiwi Monitor カラースキーム 第3版
    /// <br/>
    /// 参考: https://kiwimonitor.amebaownd.com/posts/36819100
    /// </summary>
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

    /// <summary>
    /// Quarogカラースキーム
    /// </summary>
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
    /// テキスト配色
    /// </summary>
    private static SKColor[] ForegroundColors =>
    [
        // コントラストが強い色
        SKColors.White, // 震度なし/不明/その他
        SKColors.White, // 震度1
        SKColors.White, // 震度2
        SKColors.Black, // 震度3
        SKColors.Black, // 震度4
        SKColors.Black, // 震度5弱
        SKColors.Black, // 震度5強
        SKColors.White, // 震度6弱
        SKColors.White, // 震度6強
        SKColors.White, // 震度7
        SKColors.White // 震度8(臨時)/震度7以上
    ];

    /// <summary>
    /// 色を取得する
    /// </summary>
    /// <param name="scale">震度</param>
    /// <param name="tag">カラースキーム</param>
    /// <returns></returns>
    public static SKColor GetColor(this Scale scale, string tag = "kiwi3")
    {
        return tag switch
        {
            "Kiwi3" or "kiwi3" or "k" => scale switch
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
            },
            "Quarog" or "quarog" or "q" => scale switch
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
            },
            "Fore" or "Foreground" or "fore" or "foreground" or "fg" => scale switch
            {
                Scale.Unknown => ForegroundColors[0],
                Scale.Scale1 => ForegroundColors[1],
                Scale.Scale2 => ForegroundColors[2],
                Scale.Scale3 => ForegroundColors[3],
                Scale.Scale4 => ForegroundColors[4],
                Scale.Scale5L => ForegroundColors[5],
                Scale.Scale5H => ForegroundColors[6],
                Scale.Scale6L => ForegroundColors[7],
                Scale.Scale6H => ForegroundColors[8],
                Scale.Scale7 => ForegroundColors[9],
                _ => ForegroundColors[0]
            },
            _ => scale switch
            {
                Scale.Unknown => PrimaryColors[0],
                Scale.Scale1 => PrimaryColors[1],
                Scale.Scale2 => PrimaryColors[2],
                Scale.Scale3 => PrimaryColors[3],
                Scale.Scale4 => PrimaryColors[4],
                Scale.Scale5L => PrimaryColors[5],
                Scale.Scale5H => PrimaryColors[6],
                Scale.Scale6L => PrimaryColors[7],
                Scale.Scale6H => PrimaryColors[8],
                Scale.Scale7 => PrimaryColors[9],
                Scale.Scale8 => PrimaryColors[10],
                _ => PrimaryColors[0]
            }
        };
    }
}