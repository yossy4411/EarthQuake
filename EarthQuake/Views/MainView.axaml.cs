using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EarthQuake.Views; 
public partial class MainView : UserControl
{
    private readonly DispatcherTimer timer;

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
        KmoniPanel.Children.Insert(0, graph);
#endif
        Selection.OnSelected += Selection_OnSelected;
        DateStart.SelectedDate = DateTime.Now.AddDays(-4).Date;
        DateEnd.SelectedDate = DateTime.Now.Date;
        UpdateEpic.Click += Update_Epicenters;
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        timer.Tick += Timer_Elapsed;
        timer.Start();
    }

    private void Timer_Elapsed(object? sender, EventArgs args)
    {
        if (Kmoni.IsVisible)
        {
            Kmoni.InvalidateVisual();
        }

    }

    private void Selection_OnSelected(object? sender, Canvas.SelectionEventArgs e)
    {
        Statistics.Selected = e.Selected;
        Statistics.Epicenters = App.ViewModel.Hypo.GetPoints(e.Selected).ToList();
    }

    private async void ListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Loading.IsVisible = true;
        await Task.Run(() =>
        {
            App.ViewModel.SetQInfo(Quakes.SelectedIndex);
        });
        Info.InvalidateVisual();
        Loading.IsVisible = false;
    }

    private void Slider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        Selection.Rotation = (float)Slider.Value;
        Selection.InvalidateVisual();
    }

    private void Update_Epicenters(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DateStart.SelectedDate.HasValue && DateEnd.SelectedDate.HasValue)
        {
            App.ViewModel.GetEpicenters(DateStart.SelectedDate.Value.Date, (int)(DateEnd.SelectedDate.Value.Date - DateStart.SelectedDate.Value.Date).TotalDays);
        }
        else
        {
            Debug.WriteLine("Date is not set or invalid.");
        }
    }
}
