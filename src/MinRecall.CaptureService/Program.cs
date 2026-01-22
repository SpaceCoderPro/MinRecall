using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MinRecall.CaptureService;

public class Program
{
    private static ILogger<Program>? _logger;

    public static async Task Main(string[] args)
    {
        // Setup unhandled exception handlers
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            LogFatalError("Unhandled Domain Exception", e.ExceptionObject as Exception);
            Environment.Exit(1);
        };

        try
        {
            // Check if running as Windows Service
            var isService = !Environment.UserInteractive;
            var serviceName = "MinRecall Capture Service";

            var hostBuilder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddWindowsService(options =>
                    {
                        options.ServiceName = serviceName;
                    });

                    services.AddHostedService<CaptureWorker>();
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.ClearProviders();
                    
                    // Add EventLog logging for service
                    if (isService)
                    {
                        logging.AddEventLog(eventLogSettings =>
                        {
                            eventLogSettings.SourceName = serviceName;
                        });
                    }
                    
                    // Add console logging for interactive mode
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });

            var host = hostBuilder.Build();
            _logger = host.Services.GetService(typeof(ILogger<Program>)) as ILogger<Program>;

            LogInfo($"Starting {serviceName}...");

            if (isService)
            {
                await host.RunAsync();
            }
            else
            {
                await host.RunAsync();
            }
        }
        catch (Exception ex)
        {
            LogFatalError("Fatal error in CaptureService", ex);
            
            if (!Environment.UserInteractive)
            {
                // Running as service, wait a bit before exiting
                await Task.Delay(5000);
            }
            else
            {
                // Running interactively, wait for user input
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
            
            Environment.Exit(1);
        }
    }

    private static void LogInfo(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logMessage = $"[{timestamp}] INFO: {message}";
        
        try
        {
            WriteToLogFile(logMessage);
        }
        catch { /* Ignore logging errors */ }

        Console.WriteLine(logMessage);
        _logger?.LogInformation(message);
    }

    private static void LogFatalError(string message, Exception? ex)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var errorMessage = $"[{timestamp}] FATAL: {message}";
        
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
            WriteToLogFile(errorMessage, isError: true);
        }
        catch { /* Ignore logging errors */ }

        Console.WriteLine(errorMessage);
        Debug.WriteLine(errorMessage);
        _logger?.LogCritical(ex, message);
    }

    private static void WriteToLogFile(string message, bool isError = false)
    {
        try
        {
            // Try to write to a log file in AppData
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logFolder = Path.Combine(appDataFolder, "MinRecall", "Logs");
            Directory.CreateDirectory(logFolder);
            
            var logFile = Path.Combine(logFolder, $"CaptureService_{DateTime.Now:yyyyMMdd}.log");
            var timestampedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{(isError ? "ERROR" : "INFO")}] {message}\n";
            
            File.AppendAllText(logFile, timestampedMessage);
        }
        catch
        {
            // Ignore logging errors
        }
    }
}
