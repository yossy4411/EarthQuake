using MessagePack;

namespace EarthQuake.Core.Animation;

/// <summary>
/// 線形補間された波の半径を保存するクラス
/// </summary>
/// <param name="time"></param>
[MessagePackObject]
public class InterpolatedWaveData
{
    [Key("radius")] public float[] Radius { get; init; } = [];

    [Key("wave")] public Dictionary<int, PSWave[]> Waves { get; init; } = [];

    private float GetRadius(int depth, float seconds, bool isPWave)
    {
        if (!Waves.TryGetValue(depth, out var values)) return 0;
        if (seconds <= (isPWave ? values[0].PTime : values[0].STime)) return 0;
        switch (values.Length)
        {
            case 0:
            {
                return 0;
            }
            case 1:
            {
                return 0;
            }
            default:
            {
                if (seconds <= values[0].PTime) return 0;
                for (var i = 0; i < values.Length - 1; i++)
                {
                    if (isPWave)
                    {
                        if (!(values[i].PTime <= seconds) || !(seconds <= values[i + 1].PTime)) continue;
                        var t = (seconds - values[i].PTime) / (values[i + 1].PTime - values[i].PTime);
                        return Radius[i] * (1 - t) + Radius[i + 1] * t;
                    }
                    else
                    {
                        if (!(values[i].STime <= seconds) || !(seconds <= values[i + 1].STime)) continue;
                        var t = (seconds - values[i].STime) / (values[i + 1].STime - values[i].STime);
                        return Radius[i] * (1 - t) + Radius[i + 1] * t;
                    }
                }

                return isPWave
                    ? (values[^1].PTime - seconds) * (Radius[^1] - Radius[^2]) + Radius[^2]
                    : (values[^1].STime - seconds) * (Radius[^1] - Radius[^2]) + Radius[^2];
            }
        }
    }

    /// <summary>
    /// P波の半径を取得する
    /// </summary>
    /// <param name="depth">震源の深さ</param>
    /// <param name="seconds">経過時間</param>
    /// <returns>半径</returns>
    public float GetPRadius(int depth, float seconds) => GetRadius(depth, seconds, true) / Earth * 360;

    /// <summary>
    /// S波の半径を取得する
    /// </summary>
    /// <param name="depth">震源の深さ</param>
    /// <param name="seconds">経過時間</param>
    /// <returns>半径</returns>
    public float GetSRadius(int depth, float seconds) => GetRadius(depth, seconds, false) / Earth * 360;

    private const int Earth = 40075;
}

/// <summary>
/// P波、S波の時間軸を保存するクラス
/// </summary>
[MessagePackObject]
public class PSWave
{
    [Key(0)] public float PTime { get; init; }
    [Key(1)] public float STime { get; init; }
}