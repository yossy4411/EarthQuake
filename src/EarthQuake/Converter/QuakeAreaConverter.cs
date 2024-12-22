using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using EarthQuake.Core.EarthQuakes.OGSP;

namespace EarthQuake.Converter;

public class QuakeAreaConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not IEnumerable<string> areas ? null : string.Join("、", areas) + "では、強い揺れに警戒";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}