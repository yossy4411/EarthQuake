using Avalonia.Controls;
using EarthQuake.Core.GeoJson;
using SkiaSharp;
using System.Collections.Generic;

namespace EarthQuake.Views;

public partial class StatisticsPanel : UserControl
{
    public List<Epicenters.Epicenter> Epicenters
    {
        set
        {
            A.Epicenters = value;
            B.Epicenters = value;
            C.Epicenters = value;
            A.InvalidateVisual();
            B.InvalidateVisual();
            C.InvalidateVisual();
        }
    }

    public SKRect Selected
    {
        set => RangeText.Text = $"範囲: {value.Left:F2},{value.Top:F2} - {value.Right:F2},{value.Bottom:F2}";
    }

    public StatisticsPanel()
    {
        InitializeComponent();
    }
}