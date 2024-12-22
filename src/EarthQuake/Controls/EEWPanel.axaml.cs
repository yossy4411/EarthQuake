using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using EarthQuake.Core.EarthQuakes.OGSP;

namespace EarthQuake.Controls;

public class EEWPanel : TemplatedControl
{
    public static readonly StyledProperty<EEW> EEWProperty = AvaloniaProperty.Register<EEWPanel, EEW>(
        nameof(EEW));

    public EEW EEW
    {
        get => GetValue(EEWProperty);
        set => SetValue(EEWProperty, value);
    }
}