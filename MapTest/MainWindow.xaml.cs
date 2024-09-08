using System.IO;
using System.Windows;
using System.Windows.Input;
using EarthQuake.Core.TopoJson;
using EarthQuake.Map;
using EarthQuake.Map.Layers;
using EarthQuake.Map.Tiles;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace MapTest;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        // "E:\source\EarthQuake\EarthQuake\Assets\world.mpk.lz4"から読み込む
        using var stream = new FileStream(@"E:\source\EarthQuake\EarthQuake\Assets\world.mpk.lz4", FileMode.Open);
        var worldSet = Serializer.Deserialize<WorldPolygonSet>(stream);
        var world = new CountriesLayer(worldSet);
        using var stream2 = new FileStream(@"E:\source\EarthQuake\ConsoleTest\PerformanceTest\gsi.json", FileMode.Open);
        using var reader = new StreamReader(stream2);
        var styles = VectorMapStyles.LoadGLJson(reader);
        var map = new VectorMapLayer(styles);
        Controller = new MapViewController
        {
            MapLayers = [world, map]
        };
        
    }
    public class MapCanvasTranslation
    {
        public SKPoint Translate { get; set; }
        public float Scale { get; set; } = 1f;
    }

    private MapViewController? Controller { get; set; }
    private MapCanvasTranslation Translation { get; set; } = new();
    private SKPoint Center => new((float)Width / 2, (float)Height / 2);
    private SKPoint Translate { get => Translation.Translate; set => Translation.Translate = value; }
    private float Scale { get => Translation.Scale; set => Translation.Scale = value; }
    private Point _scrollOffset;
    private bool _pressed;
    private void SKElement_OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();
        SKRect clipRect = new(0, 0, (float)Width, (float)Height);
        canvas.ClipRect(clipRect);
        var translate = Translate + Center;
        var region = new SKRect(-translate.X / Scale, -translate.Y / Scale, (float)(-translate.X + Width) / Scale, (float)(-translate.Y + Height) / Scale);
        using (new SKAutoCanvasRestore(canvas))
        {
            canvas.Translate(Translate + Center);
            canvas.Scale(Scale);

            Controller?.RenderBase(canvas, Scale, region);
        }
    }

    private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _scrollOffset = e.GetPosition(this);
        _pressed = e.LeftButton is MouseButtonState.Pressed;
    }

    private void UIElement_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_pressed) return;
        var point = _scrollOffset - e.GetPosition(this);
        _scrollOffset = e.GetPosition(this);
        Translate = new SKPoint(Translate.X - (float)point.X, Translate.Y - (float)point.Y);
        UpdateView();
    }

    private void UIElement_OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _pressed = false;
    }

    private void UIElement_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        
        var point = e.GetPosition(this);
        var zoomDelta = (float)Math.Pow(1.2f, e.Delta * 0.005);
        Scale *= zoomDelta;
        Translate = new SKPoint(Translate.X + Translate.X * (zoomDelta - 1) - ((float)point.X - Center.X) * (zoomDelta - 1), Translate.Y + Translate.Y * (zoomDelta - 1) - ((float)point.Y - Center.Y) * (zoomDelta - 1));
        UpdateView();
    }
    private void UpdateView()
    {
        SkElement.InvalidateVisual();
    }
    
}