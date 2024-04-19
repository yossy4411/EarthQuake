using Avalonia.Controls;
using Avalonia.Threading;
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
        Selection.OnSelected += Selection_OnSelected;
    }

    private void Selection_OnSelected(object? sender, Canvas.SelectionEventArgs e)
    {
        statistics.Epicenters = App.ViewModel.Hypo.GetPoints(e.Selected);
    }

    private async void ListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Loading.IsVisible = true;
        await Task.Run(() =>
        {
            App.ViewModel.SetQInfo(quakes.SelectedIndex);
        });
        info.InvalidateVisual();
        Loading.IsVisible = false;

        
    }

    private void Slider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        Selection.Rotation = (float)slider.Value;
        Selection.InvalidateVisual();
    }

}
