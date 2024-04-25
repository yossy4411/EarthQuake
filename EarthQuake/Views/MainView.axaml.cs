using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace EarthQuake.Views;

public partial class MainView : UserControl
{
#pragma warning disable IDE0052 // 読み取られていないプライベート メンバーを削除
    private readonly Timer timer;
#pragma warning restore IDE0052 // 読み取られていないプライベート メンバーを削除

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
        dateStart.SelectedDate = DateTime.Now.AddDays(-4).Date;
        dateEnd.SelectedDate = DateTime.Now.Date;
        updateEpic.Click += Update_Epicenters;
        timer = new(Timer_Elapsed, null, 0, 250);
    }

    private void Timer_Elapsed(object? state)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (kmoni.IsVisible)
            {
                kmoni.InvalidateVisual();
            }
        }
        );

    }

    private void Selection_OnSelected(object? sender, Canvas.SelectionEventArgs e)
    {
        statistics.Selected = e.Selected;
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

    private void Update_Epicenters(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (dateStart.SelectedDate.HasValue && dateEnd.SelectedDate.HasValue)
        {
            App.ViewModel.GetEpicenters(((DateTime)dateStart.SelectedDate).Date, (int)(((DateTime)dateEnd.SelectedDate).Date - ((DateTime)dateStart.SelectedDate).Date).TotalDays);
        }
    }
}
