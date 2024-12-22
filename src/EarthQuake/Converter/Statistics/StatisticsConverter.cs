using Avalonia.Data.Converters;
using SkiaSharp;
using System;
using System.Globalization;

namespace EarthQuake.Converter.Statistics;

/// <summary>
/// SKRectを文字列に変換するコンバーター
/// </summary>
public class RectangleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var rect = value as SKRect? ?? SKRect.Empty;
        return $"範囲: ({rect.Left}, {rect.Top}) - ({rect.Right}, {rect.Bottom})";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return SKRect.Empty;
    }
}