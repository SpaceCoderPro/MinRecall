using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Diagnostics;

namespace MinRecall.UI;

// The main program class for Avalonia applications
public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any other SynchronizationContext-reliant code before AppMain is called.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Setup unhandled exception handlers
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                LogError("Unhandled Exception", e.ExceptionObject as Exception);
                Environment.Exit(1);
            };

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogError("Fatal Error starting application", ex);
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
            Environment.Exit(1);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    private static void LogError(string message, Exception? ex)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var errorMessage = $"[{timestamp}] {message}";
        
        if (ex != null)
        {
            errorMessage += $"\n\nException Type: {ex.GetType().FullName}";
            errorMessage += $"\nMessage: {ex.Message}";
            errorMessage += $"\n\nStack Trace:\n{ex.StackTrace}";
            
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nInner Exception: {ex.InnerException.GetType().FullName}";
                errorMessage += $"\nInner Message: {ex.InnerException.Message}";
            }
        }
        
        try
        {
            // Try to write to a log file in AppData
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logFolder = System.IO.Path.Combine(appDataFolder, "MinRecall", "Logs");
            System.IO.Directory.CreateDirectory(logFolder);
            
            var logFile = System.IO.Path.Combine(logFolder, $"error_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            System.IO.File.WriteAllText(logFile, errorMessage);
            
            Console.WriteLine($"Error details written to: {logFile}");
        }
        catch
        {
            // Ignore logging errors
        }
        
        Console.WriteLine(errorMessage);
        Debug.WriteLine(errorMessage);
    }
}