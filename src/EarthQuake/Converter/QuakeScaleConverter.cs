using System;
using System.Globalization;
using Avalonia.Data.Converters;
using EarthQuake.Core.EarthQuakes;
using EarthQuake.Map.Colors;

namespace EarthQuake.Converter;

/// <summary>
/// 震度を表示するコンバーター
/// </summary>
public class QuakeScaleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not Scale scale ? null : scale.ToScreenString(true);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not string text ? null : ScaleConverter.FromString(text);
    }
}

public class QuakeColorConverter : IValueConverter
{
    public string ColorScheme { get; set; } = "Kiwi3";
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not Scale scale ? null : scale.GetColor(ColorScheme).GetBrush();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}