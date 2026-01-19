using System.Diagnostics;

namespace MinRecall.CaptureService.Utilities;

public static class CpuMonitor
{
    private static PerformanceCounter? _cpuCounter;
    private static DateTime _lastCheck = DateTime.MinValue;
    private static float _lastCpuUsage = 0;

    static CpuMonitor()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            // First call always returns 0, so we initialize it
            _cpuCounter.NextValue();
        }
        catch
        {
            // Fall back to alternative method if PerformanceCounter fails
        }
    }

    public static float GetCpuUsage()
    {
        try
        {
            if (_cpuCounter != null)
            {
                // Only update every 200ms to avoid performance impact
                if ((DateTime.UtcNow - _lastCheck).TotalMilliseconds >= 200)
                {
                    _lastCpuUsage = _cpuCounter.NextValue();
                    _lastCheck = DateTime.UtcNow;
                }
                return _lastCpuUsage;
            }
        }
        catch
        {
            // Fallback to process-based estimation
        }

        // Fallback: estimate CPU usage by checking all processes
        return EstimateCpuUsage();
    }

    private static float EstimateCpuUsage()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var startCpu = Process.GetProcesses().Sum(p => GetProcessCpuTime(p));

            Thread.Sleep(100);

            var endTime = DateTime.UtcNow;
            var endCpu = Process.GetProcesses().Sum(p => GetProcessCpuTime(p));

            var elapsedCpu = (endCpu - startCpu).TotalMilliseconds;
            var elapsedTotal = (endTime - startTime).TotalMilliseconds * Environment.ProcessorCount;

            return (float)Math.Min(100, (elapsedCpu / elapsedTotal) * 100);
        }
        catch
        {
            return 50f; // Conservative default
        }
    }

    private static TimeSpan GetProcessCpuTime(Process process)
    {
        try
        {
            return process.TotalProcessorTime;
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    public static bool IsCpuBelowThreshold(float threshold)
    {
        return GetCpuUsage() < threshold;
    }

    public static int GetAdaptiveQuality(int baseQuality)
    {
        var cpuUsage = GetCpuUsage();

        // Adaptive quality based on CPU usage
        if (cpuUsage < 30)
        {
            return baseQuality; // Full quality when CPU is free
        }
        else if (cpuUsage < 60)
        {
            return Math.Max(70, baseQuality - 10); // Slightly reduce quality
        }
        else if (cpuUsage < 85)
        {
            return Math.Max(60, baseQuality - 20); // More aggressive reduction
        }
        else
        {
            return 50; // Minimum quality under heavy load
        }
    }
}
