using Avalonia.Data.Converters;
using EarthQuake.Core.EarthQuakes.P2PQuake;
using System;
using Avalonia.Controls;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Shapes;
using EarthQuake.Map.Colors;

namespace EarthQuake.Converter;

/// <summary>
/// 地震リストを表示するコンバーター
/// </summary>
public class QuakeListConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        DockPanel panel = new();
        if (value is not PQuakeData quakeData) return panel;
        var color = quakeData.EarthQuake.MaxScale.GetKiwi3Color();
        var rect = new Rectangle
        {
            Width = 30,
            Height = 40,
            RadiusX = 5,
            RadiusY = 5,
            Fill = color.GetBrush(),
            Stroke = color.IncreaseBrightness(-30).GetBrush(),
            StrokeThickness = 4,
            Margin = new Thickness(5),
        };
        DockPanel.SetDock(rect, Dock.Left);
        panel.Children.Add(rect);

        if (quakeData.EarthQuake.Hypocenter is null) return panel;
        var text = new TextBlock { Text = quakeData.EarthQuake.Hypocenter.Name };
        DockPanel.SetDock(text, Dock.Right);
        panel.Children.Add(text);
        return panel;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}