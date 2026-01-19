using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace MinRecall.Optimizer;

public class Program
{
    public static async Task Main(string[] args)
    {
        var isService = !Environment.UserInteractive;

        var hostBuilder = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddWindowsService(options =>
                {
                    options.ServiceName = "MinRecall Optimizer Service";
                });

                services.AddHostedService<OptimizationWorker>();
            })
            .ConfigureLogging((hostContext, logging) =>
            {
                logging.ClearProviders();
                logging.AddEventLog(eventLogSettings =>
                {
                    eventLogSettings.SourceName = "MinRecall Optimizer Service";
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
