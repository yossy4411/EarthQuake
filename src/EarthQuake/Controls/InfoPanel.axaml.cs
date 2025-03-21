using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using EarthQuake.Converter;
using EarthQuake.Core.EarthQuakes;
using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Map.Colors;

namespace EarthQuake.Controls;

public partial class InfoPanel : UserControl
{
    public InfoPanel()
    {
        InitializeComponent();
    }
    
    public void SetData(PQuakeData data)
    {
        var parent = Content.Children;
        parent.Clear();
        var title = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0,0,0,10) };
        title.Inlines!.Add(new Run(data.EarthQuake.Hypocenter?.Name ?? "不明") { FontWeight = FontWeight.Black, FontSize = 18 });
        title.Inlines!.Add(new Run(" で地震がありました。") { FontWeight = FontWeight.Bold, FontSize = 12 });
        parent.Add(title);
        
        var time = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Center };
        time.Inlines!.Add(new Run($"{data.EarthQuake.Time:MM月dd日 H時mm分}") { FontWeight = FontWeight.Bold, FontSize = 12 });
        time.Inlines.Add(new Run(" に発生") { FontWeight = FontWeight.Regular, FontSize = 12 });
        parent.Add(time);
        
        var hypo = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Center };
        hypo.Inlines!.Add(new Run($"M{data.EarthQuake.Hypocenter?.Magnitude:F1}") { FontWeight = FontWeight.Bold, FontSize = 22 });
        hypo.Inlines.Add(new Run(", 深さ") { FontWeight = FontWeight.Bold, FontSize = 12 });
        hypo.Inlines.Add(new Run(data.EarthQuake.Hypocenter?.Depth == 0 ? "ごく浅い" : $"{data.EarthQuake.Hypocenter?.Depth:F1}km") { FontWeight = FontWeight.Bold, FontSize = 15 });
        parent.Add(hypo);
        
        var intensity = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(10) };
        intensity.Inlines!.Add(new Run("最大震度") { FontWeight = FontWeight.Bold, FontSize = 12 });
        intensity.Inlines.Add(new Run($" {data.EarthQuake.MaxScale.ToScreenString()} ")
        {
            FontWeight = FontWeight.Bold, FontSize = 15,
            Foreground = data.EarthQuake.MaxScale.GetColor("fg").GetBrush(),
            Background = data.EarthQuake.MaxScale.GetColor().GetBrush(),
        });
        intensity.Inlines.Add(new Run("を") { FontWeight = FontWeight.Bold, FontSize = 12 });
        var areas = data.Points?.Where(x => x.Scale == data.EarthQuake.MaxScale).Select(x => x.Addr).ToList();
        if (areas is not null && areas.Count > 0)
        {
            if (areas.Count > 3)
            {
                var area = areas.GetRange(0, 3);
                intensity.Inlines.Add(new Run($"{string.Join(", ", area)}などの観測点で観測") { FontWeight = FontWeight.Bold, FontSize = 12 });
            }
            else
            {
                intensity.Inlines.Add(new Run($"{string.Join(", ", areas)}で観測") { FontWeight = FontWeight.Bold, FontSize = 12 });
            }
        }
        else
        {
            intensity.Inlines.Add(new Run("{Unknown Area} で観測") { FontWeight = FontWeight.Bold, FontSize = 12 });
        }
        parent.Add(intensity);
        
    }
}