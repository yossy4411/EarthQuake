using Avalonia.Controls;
using System.Threading.Tasks;

namespace EarthQuake.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
#if !DEBUG
        var graph = new ShindoGraph()
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            Height = 100,
            ClipToBounds = true,
        };
        DockPanel.SetDock(graph, Dock.Bottom);
        kmoniPanel.Children.Insert(0, graph);
#endif
    }
    private async void ListBox_SelectionChanged_1(object? sender, SelectionChangedEventArgs e)
    {
        Loading.IsVisible = true;
        await Task.Run(() => App.ViewModel.SetQInfo(quakes.SelectedIndex));
        Loading.IsVisible = false;
        info.InvalidateVisual();
    }

    private void Slider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        scope.Rotation = (float)slider.Value;
        scope.InvalidateVisual();
    }
}
