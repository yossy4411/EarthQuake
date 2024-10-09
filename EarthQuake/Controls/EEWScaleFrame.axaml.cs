using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using EarthQuake.Core.EarthQuakes;

namespace EarthQuake.Controls;

public class EEWScaleFrame : TemplatedControl
{
    public static readonly StyledProperty<Scale> ScaleProperty = AvaloniaProperty.Register<EEWScaleFrame, Scale>(
        nameof(Scale));

    public Scale Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public static readonly StyledProperty<string> AreaNameProperty = AvaloniaProperty.Register<EEWScaleFrame, string>(
        nameof(AreaName));

    public string AreaName
    {
        get => GetValue(AreaNameProperty);
        set => SetValue(AreaNameProperty, value);
    }
}