using MessagePack;

namespace EarthQuake.Core.EarthQuakes.OGSP;

[MessagePackObject]
public class EEW
{
    [Key("type")]
    public string Type { get; set; } = "eew"; // デフォルト: "eew"

    [Key("title")]
    public string? Title { get; set; } // タイトル

    [Key("serial")]
    public int Serial { get; set; } // 何報目か

    [Key("announced")]
    public long Announced { get; set; } // 緊急地震速報が発表された時刻

    [Key("origin")]
    public long Origin { get; set; } // 地震が発生した時刻

    [Key("hypocenter")]
    public Hypocenter Hypocenter { get; set; } // 震源地

    [Key("source")] public string Source { get; set; } = "不明"; // 発表元

    [Key("warn_area")]
    public WarnArea[]? WarnArea { get; set; } // 警報が出された地域

    [Key("is_sea")]
    public bool IsSea { get; set; } // 海上かどうか

    [Key("is_cancel")]
    public bool IsCancel { get; set; } // キャンセルかどうか

    [Key("is_final")]
    public bool IsFinal { get; set; } // 最終報かどうか

    [Key("is_warning")]
    public bool IsWarning { get; set; } // 警報かどうか

    [Key("is_training")]
    public bool IsTraining { get; set; } // 訓練かどうか

    [Key("assumption")]
    public bool Assumption { get; set; } // 仮定震源かどうか (PLUM Level Normal=仮定震源ではない のどれか)
}

[MessagePackObject]
public class Hypocenter
{
    [Key("name")]
    public string Name { get; set; } // 名前

    [Key("latitude")]
    public double Lat { get; set; } // 緯度

    [Key("longitude")]
    public double Lon { get; set; } // 経度

    [Key("magnitude")]
    public double Mag { get; set; } // マグニチュード

    [Key("depth")]
    public double Depth { get; set; } // 深さ

    [Key("max_int")]
    public Scale MaxInt { get; set; } // 最大震度
}

[MessagePackObject]
public class WarnArea
{
    [Key("area_code")]
    public int AreaCode { get; set; } // 地域コード

    [Key("min_int")]
    public Scale MinInt { get; set; } // 最低震度

    [Key("max_int")]
    public Scale MaxInt { get; set; } // 最高震度

    [Key("time")]
    public long Arrival { get; set; } // 予想到達時刻

    [Key("type")]
    public string Type { get; set; } // 予想到達時刻の種類
}