using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EarthQuake.Core.GeoJson;
using EarthQuake.ViewModels;
using System.Collections;
using System.Collections.Generic;

namespace EarthQuake.Views;

public partial class StatisticsPanel : UserControl
{
    private readonly StatisticsViewModel viewModel = new();
    public IEnumerable<Epicenters.Epicenter> Epicenters
    {
        get => a.Epicenters; 
        set
        {
            a.Epicenters = value;
            a.InvalidateVisual();
        }
    }
    public StatisticsPanel()
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}