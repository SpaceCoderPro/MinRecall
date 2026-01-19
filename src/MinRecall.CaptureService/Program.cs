using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace MinRecall.CaptureService;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Check if running as Windows Service
        var isService = !Environment.UserInteractive;

        var hostBuilder = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddWindowsService(options =>
                {
                    options.ServiceName = "MinRecall Capture Service";
                });

                services.AddHostedService<CaptureWorker>();
            })
            .ConfigureLogging((hostContext, logging) =>
            {
                logging.ClearProviders();
                logging.AddEventLog(eventLogSettings =>
                {
                    eventLogSettings.SourceName = "MinRecall Capture Service";
                });

                logging.SetMinimumLevel(LogLevel.Information);
            });

        if (isService)
        {
            await hostBuilder.RunAsServiceAsync();
        }
        else
        {
            await hostBuilder.RunConsoleAsync();
        }
    }
}
