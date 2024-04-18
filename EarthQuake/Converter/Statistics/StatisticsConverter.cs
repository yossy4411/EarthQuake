using Avalonia.Data.Converters;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Converter.Statistics
{

    public class RectangleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            SKRect rect = value as SKRect? ?? SKRect.Empty;
            return $"範囲: ({rect.Left}, {rect.Top}) - ({rect.Right}, {rect.Bottom})";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
}
