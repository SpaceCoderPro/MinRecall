using MinRecall.CaptureService.WindowsApi;
using MinRecall.CaptureService.Utilities;
using MinRecall.Core;
using MinRecall.Core.Database;
using MinRecall.Core.Models;
using MinRecall.Core.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MinRecall.CaptureService;

public class CaptureWorker : BackgroundService
{
    private readonly ILogger<CaptureWorker> _logger;
    private readonly DatabaseManager _database;
    private readonly SettingsManager _settings;
    private DateTime? _lastWindowChangeTime;
    private DateTime? _lastCaptureTime;
    private string _lastProcessName = string.Empty;
    private string _lastWindowTitle = string.Empty;
    private int _screenshotsSinceKeyframe = 0;
    private readonly object _captureLock = new();

    public CaptureWorker(ILogger<CaptureWorker> logger)
    {
        _logger = logger;

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MinRecall"
        );

        _database = new DatabaseManager(Path.Combine(appDataPath, "minrecall.db"));
        _settings = new SettingsManager();
        _settings.ApplyProfileToSettings();

        EnsureStorageDirectory();
    }

    private void EnsureStorageDirectory()
    {
        if (!Directory.Exists(_settings.Settings.StorageLocation))
        {
            Directory.CreateDirectory(_settings.Settings.StorageLocation);
        }

        // Create monthly subdirectories
        var monthPath = Path.Combine(
            _settings.Settings.StorageLocation,
            DateTime.UtcNow.ToString("yyyy-MM")
        );

        if (!Directory.Exists(monthPath))
        {
            Directory.CreateDirectory(monthPath);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MinRecall Capture Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CaptureScreenshot(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing screenshot");
            }

            var delay = TimeSpan.FromSeconds(_settings.Settings.CaptureIntervalSeconds);
            await Task.Delay(delay, stoppingToken);
        }

        _logger.LogInformation("MinRecall Capture Service stopped");
    }

    private async Task CaptureScreenshot(CancellationToken stoppingToken)
    {
        lock (_captureLock)
        {
            var windowInfo = WindowCapture.GetForegroundWindowInfo();
            if (windowInfo == null)
            {
                return;
            }

            // Skip if in privacy blacklist
            if (IsBlacklisted(windowInfo))
            {
                _logger.LogDebug("Skipping blacklisted window: {ProcessName}", windowInfo.ProcessName);
                UpdateActivityLog(null);
                return;
            }

            // Skip desktop windows
            if (WindowCapture.IsDesktopWindow(windowInfo.Handle))
            {
                return;
            }

            // Check for window change
            var windowChanged = windowInfo.ProcessName != _lastProcessName ||
                               windowInfo.Title != _lastWindowTitle;

            if (windowChanged)
            {
                FinalizeActivityLog();
                _lastProcessName = windowInfo.ProcessName;
                _lastWindowTitle = windowInfo.Title;
                _lastWindowChangeTime = DateTime.UtcNow;
            }

            var shouldCapture = ShouldCaptureScreenshot(windowChanged);
            if (!shouldCapture)
            {
                return;
            }

            // Determine adaptive quality
            var jpegQuality = _settings.Settings.AdaptiveQuality
                ? CpuMonitor.GetAdaptiveQuality(_settings.Settings.JpegQuality)
                : _settings.Settings.JpegQuality;

            // Capture the window
            var bitmap = WindowCapture.CaptureWindow(
                windowInfo.Handle,
                _settings.Settings.TargetWidth,
                _settings.Settings.TargetHeight,
                jpegQuality
            );

            if (bitmap == null)
            {
                _logger.LogWarning("Failed to capture window: {Title}", windowInfo.Title);
                return;
            }

            // Determine if this is a keyframe
            var isKeyframe = _screenshotsSinceKeyframe >= _settings.Settings.KeyframeFrequency - 1;
            if (isKeyframe)
            {
                _screenshotsSinceKeyframe = 0;
            }
            else
            {
                _screenshotsSinceKeyframe++;
            }

            // Save screenshot
            var screenshot = SaveScreenshot(bitmap, windowInfo, isKeyframe);
            if (screenshot != null)
            {
                _lastCaptureTime = screenshot.Timestamp;
                UpdateActivityLog(screenshot);
            }

            bitmap.Dispose();
        }

        await Task.CompletedTask;
    }

    private Screenshot? SaveScreenshot(Bitmap bitmap, WindowCapture.WindowInfo windowInfo, bool isKeyframe)
    {
        try
        {
            var monthPath = Path.Combine(
                _settings.Settings.StorageLocation,
                DateTime.UtcNow.ToString("yyyy-MM")
            );

            var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{windowInfo.ProcessName}.png";
            var filePath = Path.Combine(monthPath, fileName);

            // Save as PNG initially (will be optimized later)
            bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

            var fileInfo = new FileInfo(filePath);
            var screenshot = new Screenshot
            {
                Timestamp = DateTime.UtcNow,
                WindowTitle = TruncateString(windowInfo.Title, 500),
                ProcessName = windowInfo.ProcessName,
                FilePath = filePath,
                IsKeyframe = isKeyframe,
                Width = _settings.Settings.TargetWidth,
                Height = _settings.Settings.TargetHeight,
                FileSize = fileInfo.Length,
                Status = ScreenshotStatus.Pending
            };

            var id = _database.InsertScreenshot(screenshot);
            screenshot.Id = id;

            _logger.LogDebug("Captured screenshot {Id}: {ProcessName} - {Title}",
                id, windowInfo.ProcessName, windowInfo.Title);

            return screenshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving screenshot");
            return null;
        }
    }

    private bool ShouldCaptureScreenshot(bool windowChanged)
    {
        if (_lastCaptureTime == null)
        {
            return true;
        }

        var timeSinceLastCapture = DateTime.UtcNow - _lastCaptureTime.Value;

        if (timeSinceLastCapture.TotalSeconds >= _settings.Settings.CaptureIntervalSeconds)
        {
            return true;
        }

        return windowChanged && timeSinceLastCapture.TotalSeconds >= 10;
    }

    private void UpdateActivityLog(Screenshot? screenshot)
    {
        try
        {
            var now = DateTime.UtcNow;
            var currentProcessName = _lastProcessName;
            var currentWindowTitle = _lastWindowTitle;

            if (string.IsNullOrEmpty(currentProcessName))
            {
                return;
            }

            if (_lastWindowChangeTime == null)
            {
                _lastWindowChangeTime = now;
                return;
            }

            // Look for existing activity log for current window
            var activityLogs = _database.GetActivityLog(
                _lastWindowChangeTime.Value,
                now,
                currentProcessName
            );

            if (activityLogs.Count > 0)
            {
                var lastLog = activityLogs.OrderByDescending(l => l.EndTime).First();

                // Update existing log if it's recent (within 5 minutes)
                if ((now - lastLog.EndTime).TotalMinutes < 5)
                {
                    lastLog.EndTime = now;
                    lastLog.DurationSeconds = (long)(now - lastLog.StartTime).TotalSeconds;
                    if (screenshot != null)
                    {
                        lastLog.ScreenshotCount++;
                    }
                    _database.UpsertActivityLog(lastLog);
                    return;
                }
            }

            // Create new activity log
            if (screenshot != null)
            {
                var activityLog = new ActivityLog
                {
                    ProcessName = currentProcessName,
                    WindowTitle = TruncateString(currentWindowTitle, 500),
                    StartTime = _lastWindowChangeTime.Value,
                    EndTime = now,
                    DurationSeconds = (long)(now - _lastWindowChangeTime.Value).TotalSeconds,
                    ScreenshotCount = 1
                };

                _database.UpsertActivityLog(activityLog);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating activity log");
        }
    }

    private void FinalizeActivityLog()
    {
        try
        {
            if (_lastWindowChangeTime == null || string.IsNullOrEmpty(_lastProcessName))
            {
                return;
            }

            var now = DateTime.UtcNow;
            var activityLogs = _database.GetActivityLog(
                _lastWindowChangeTime.Value,
                now,
                _lastProcessName
            );

            if (activityLogs.Count > 0)
            {
                var lastLog = activityLogs.OrderByDescending(l => l.EndTime).First();
                lastLog.EndTime = now;
                lastLog.DurationSeconds = (long)(now - lastLog.StartTime).TotalSeconds;
                _database.UpsertActivityLog(activityLogs[0]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing activity log");
        }
    }

    private bool IsBlacklisted(WindowCapture.WindowInfo windowInfo)
    {
        return _settings.Settings.PrivacyBlacklist.Any(app =>
            app.Equals(windowInfo.ProcessName, StringComparison.OrdinalIgnoreCase) ||
            windowInfo.Title.Contains(app, StringComparison.OrdinalIgnoreCase)
        );
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength);
    }
}
