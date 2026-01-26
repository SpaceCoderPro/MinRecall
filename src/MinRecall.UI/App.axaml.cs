using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;

namespace MinRecall.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        try
        {
            Console.WriteLine("[DEBUG] App.Initialize() starting...");
            AvaloniaXamlLoader.Load(this);
            Console.WriteLine("[DEBUG] App.Initialize() completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] App.Initialize() failed:");
            Console.WriteLine($"  Type: {ex.GetType().FullName}");
            Console.WriteLine($"  Message: {ex.Message}");
            Console.WriteLine($"  StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            Console.WriteLine("[DEBUG] OnFrameworkInitializationCompleted starting...");
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Console.WriteLine("[DEBUG] Creating MainWindow...");
                var mainWindow = new Views.MainWindow();
                
                if (mainWindow == null)
                {
                    Console.WriteLine("[ERROR] MainWindow is null!");
                    throw new InvalidOperationException("MainWindow failed to create");
                }
                
                desktop.MainWindow = mainWindow;
                Console.WriteLine("[DEBUG] MainWindow created and assigned successfully");
            }
            else
            {
                Console.WriteLine("[ERROR] ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime");
            }

            base.OnFrameworkInitializationCompleted();
            Console.WriteLine("[DEBUG] OnFrameworkInitializationCompleted completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] OnFrameworkInitializationCompleted failed:");
            Console.WriteLine($"  Type: {ex.GetType().FullName}");
            Console.WriteLine($"  Message: {ex.Message}");
            Console.WriteLine($"  StackTrace: {ex.StackTrace}");
            throw;
        }
    }
}