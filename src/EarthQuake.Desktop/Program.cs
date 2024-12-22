using System;

using Avalonia;
using Avalonia.Media;
using Avalonia.ReactiveUI;

namespace EarthQuake.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .With(new FontManagerOptions()
            {
                //DefaultFamilyName = "avares://EarthQuake/Assets/Fonts/NotoSansJP-VariableFont_wght.ttf#Noto Sans JP Medium",
                DefaultFamilyName = "avares://EarthQuake/Assets/Fonts/#Noto Sans JP",
                FontFallbacks =
                [
                    new FontFallback { FontFamily = "avares://EarthQuake/Assets/Fonts/#Noto Sans JP" }
                ]
            })
            .UseSkia()
            .UseReactiveUI();
}
