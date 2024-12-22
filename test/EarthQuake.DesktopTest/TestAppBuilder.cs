using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using EarthQuake.Canvas;
using EarthQuake.Views;

namespace EarthQuake.DesktopTest;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
    
    [AvaloniaFact]
    public async Task Test_Canvas_Map()
    {
        var app = BuildAvaloniaApp();
        var window = new MainWindow();

        var mainView = window.FindControl<MainView>("MainView");
        Assert.NotNull(mainView);
        
        var tab = mainView.FindControl<TabControl>("Tab");
        Assert.NotNull(tab);
        
        tab.SelectedIndex = 0;
        var canvas = mainView.FindControl<MapCanvas>("Kmoni");
        Assert.NotNull(canvas);
        
        canvas.InvalidateVisual();
        
        // メインの処理を実行しながら5秒待機
        await Task.Delay(5000);
        
        // もしアプリが使用するメモリが200MBを超えた場合、警告を出す。
        // ヘッドレスで実行しているので殆どの場合超えることはないのですが、もしメモリリークが発生した場合に備えて。
        if (GC.GetTotalMemory(false) > 200 * 1024 * 1024)
        {
            Assert.Fail("Memory usage is too high");
        }
        
        // すべて通過なら成功！よかったね（´・ω・｀）
    }
}