using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace EarthQuake.Converter;

/// <summary>
/// エポック時間を変換するコンバーター
/// </summary>
public class EpochTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long epoch) return null;
        return DateTimeOffset.FromUnixTimeMilliseconds(epoch).LocalDateTime.ToString("HH:mm:ss");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}