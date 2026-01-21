using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace MinRecall.UI;

// The main program class for Avalonia applications
public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any other SynchronizationContext-reliant code before AppMain is called.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}