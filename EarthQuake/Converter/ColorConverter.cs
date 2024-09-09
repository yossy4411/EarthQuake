﻿using Avalonia.Media;
using SkiaSharp;

namespace EarthQuake.Converter;

public static class ColorConverter
{
    public static IBrush GetBrush(this SKColor color)
    {
        return new SolidColorBrush(new Color(color.Alpha, color.Red, color.Green, color.Blue));
    }
}