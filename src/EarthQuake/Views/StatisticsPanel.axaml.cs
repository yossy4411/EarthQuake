using Avalonia.Controls;
using EarthQuake.Core.GeoJson;
using EarthQuake.ViewModels;
using SkiaSharp;
using System.Collections.Generic;

namespace EarthQuake.Views;

public partial class StatisticsPanel : UserControl
{
    private readonly StatisticsViewModel viewModel = new();
    public List<Epicenters.Epicenter> Epicenters
    {
        set
        {
            A.Epicenters = value;
            B.Epicenters = value;
            C.Epicenters = value;
            A.InvalidateVisual();
            B.InvalidateVisual();
            C.InvalidateVisual();
        }
    }
    public SKRect Selected { get => viewModel.Range; set => viewModel.Range = value; }
    public StatisticsPanel()
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}