using Avalonia.Controls;
using EarthQuake.Core.GeoJson;
using SkiaSharp;
using System.Collections.Generic;

namespace EarthQuake.Views;

public partial class StatisticsPanel : UserControl
{
    /// <summary>
    /// 表示する震央のリストを設定します
    /// </summary>
    /// <param name="epicenters">震央リスト</param>
    /// <param name="bounds">範囲</param>
    public void Select(List<Epicenters.Epicenter> epicenters, SKRect bounds)
    {
        A.SetEpicenters(epicenters);
        B.SetEpicenters(epicenters);
        C.SetEpicenters(epicenters);
        RangeText.Text = $"範囲: {bounds.Left:F2},{bounds.Top:F2} - {bounds.Right:F2},{bounds.Bottom:F2}";
    }

    public StatisticsPanel()
    {
        InitializeComponent();
    }
}