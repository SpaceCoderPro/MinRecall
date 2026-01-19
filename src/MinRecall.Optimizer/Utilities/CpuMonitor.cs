using System.Diagnostics;

namespace MinRecall.Optimizer.Utilities;

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
            _cpuCounter.NextValue();
        }
        catch
        {
            // Fall back to alternative method
        }
    }

    public static float GetCpuUsage()
    {
        try
        {
            if (_cpuCounter != null)
            {
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
            // Fallback
        }

        return EstimateCpuUsage();
    }

    private static float EstimateCpuUsage()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var startCpuTicks = Process.GetProcesses().Sum(p => GetProcessCpuTime(p).Ticks);

            Thread.Sleep(100);

            var endTime = DateTime.UtcNow;
            var endCpuTicks = Process.GetProcesses().Sum(p => GetProcessCpuTime(p).Ticks);

            var elapsedCpu = TimeSpan.FromTicks(endCpuTicks - startCpuTicks).TotalMilliseconds;
            var elapsedTotal = (endTime - startTime).TotalMilliseconds * Environment.ProcessorCount;

            return (float)Math.Min(100, (elapsedCpu / elapsedTotal) * 100);
        }
        catch
        {
            return 50f;
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
}
