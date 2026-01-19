using MinRecall.Core;
using MinRecall.Core.Database;
using MinRecall.Core.Models;
using MinRecall.Core.Settings;
using MinRecall.Optimizer.ImageCompression;
using MinRecall.Optimizer.Ocr;
using MinRecall.Optimizer.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MinRecall.Optimizer;

public class OptimizationWorker : BackgroundService
{
    private readonly ILogger<OptimizationWorker> _logger;
    private readonly DatabaseManager _database;
    private readonly SettingsManager _settings;

    public OptimizationWorker(ILogger<OptimizationWorker> logger)
    {
        _logger = logger;

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MinRecall"
        );

        _database = new DatabaseManager(Path.Combine(appDataPath, "minrecall.db"));
        _settings = new SettingsManager();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MinRecall Optimizer Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingScreenshots(stoppingToken);
                await PerformMaintenance(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in optimization worker");
            }

            // Wait before checking again
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("MinRecall Optimizer Service stopped");
    }

    private async Task ProcessPendingScreenshots(CancellationToken stoppingToken)
    {
        // Check CPU usage
        if (!CpuMonitor.IsCpuBelowThreshold(_settings.Settings.OptimizationCpuThreshold))
        {
            _logger.LogDebug("CPU usage too high, skipping optimization");
            return;
        }

        var pendingScreenshots = _database.GetPendingScreenshots(limit: 5);
        if (!pendingScreenshots.Any())
        {
            return;
        }

        _logger.LogInformation("Processing {Count} pending screenshots", pendingScreenshots.Count);

        foreach (var screenshot in pendingScreenshots)
        {
            try
            {
                await ProcessScreenshot(screenshot, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing screenshot {Id}", screenshot.Id);
                _database.UpdateScreenshotStatus(screenshot.Id, ScreenshotStatus.Failed);
            }
        }
    }

    private async Task ProcessScreenshot(Screenshot screenshot, CancellationToken stoppingToken)
    {
        // Mark as optimizing
        _database.UpdateScreenshotStatus(screenshot.Id, ScreenshotStatus.Optimizing);

        // Load the PNG screenshot
        using var bitmap = new Bitmap(screenshot.FilePath);
        if (bitmap == null)
        {
            _logger.LogWarning("Could not load screenshot {Id}", screenshot.Id);
            _database.UpdateScreenshotStatus(screenshot.Id, ScreenshotStatus.Failed);
            return;
        }

        if (screenshot.IsKeyframe)
        {
            // Process as keyframe - compress to AVIF/JPEG
            await ProcessKeyframe(screenshot, bitmap);
        }
        else
        {
            // Process as delta - calculate and store delta
            await ProcessDelta(screenshot, bitmap);
        }

        // Perform OCR if enabled
        if (_settings.Settings.BackgroundOcr)
        {
            await ProcessOcr(screenshot, bitmap);
        }

        // Clean up original PNG if optimization succeeded
        try
        {
            var optimizedPath = screenshot.FilePath.Replace(".png", "_optimized.jpg", StringComparison.OrdinalIgnoreCase);
            if (File.Exists(optimizedPath))
            {
                File.Delete(screenshot.FilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete original PNG for screenshot {Id}", screenshot.Id);
        }
    }

    private async Task ProcessKeyframe(Screenshot screenshot, Bitmap bitmap)
    {
        try
        {
            // Compress to AVIF (or JPEG as fallback)
            var compressedData = await AvifCompressor.CompressToAvifAsync(bitmap, quality: 85);

            if (compressedData == null)
            {
                _logger.LogWarning("Failed to compress keyframe {Id}", screenshot.Id);
                return;
            }

            // Save compressed keyframe
            var optimizedPath = screenshot.FilePath.Replace(".png", "_optimized.jpg", StringComparison.OrdinalIgnoreCase);
            await File.WriteAllBytesAsync(optimizedPath, compressedData);

            var fileInfo = new FileInfo(optimizedPath);

            // Update database
            _database.UpdateScreenshotStatus(screenshot.Id, ScreenshotStatus.Optimized, DateTime.UtcNow);

            // Update file path and size
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_database.DatabasePath}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Screenshots
                SET FilePath = @filePath, FileSize = @fileSize
                WHERE Id = @id
            ";
            command.Parameters.AddWithValue("@filePath", optimizedPath);
            command.Parameters.AddWithValue("@fileSize", fileInfo.Length);
            command.Parameters.AddWithValue("@id", screenshot.Id);
            command.ExecuteNonQuery();

            _logger.LogDebug("Optimized keyframe {Id}: {OriginalSize} -> {NewSize}",
                screenshot.Id, screenshot.FileSize, fileInfo.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing keyframe {Id}", screenshot.Id);
            throw;
        }
    }

    private async Task ProcessDelta(Screenshot screenshot, Bitmap bitmap)
    {
        try
        {
            // Get the last keyframe
            var keyframe = _database.GetLastKeyframe();
            if (keyframe == null || !File.Exists(keyframe.FilePath))
            {
                // If no keyframe exists, treat this as a keyframe
                await ProcessKeyframe(screenshot, bitmap);
                return;
            }

            using var keyframeBitmap = new Bitmap(keyframe.FilePath);
            if (keyframeBitmap == null)
            {
                await ProcessKeyframe(screenshot, bitmap);
                return;
            }

            // Calculate delta
            var deltaFile = DeltaCalculator.CalculateDelta(bitmap, keyframeBitmap);
            if (deltaFile == null)
            {
                // Delta calculation failed, treat as keyframe
                await ProcessKeyframe(screenshot, bitmap);
                return;
            }

            // Serialize and save delta
            var deltaBytes = DeltaCalculator.SerializeDelta(deltaFile);
            var deltaPath = screenshot.FilePath.Replace(".png", ".delta", StringComparison.OrdinalIgnoreCase);
            await File.WriteAllBytesAsync(deltaPath, deltaBytes);

            var fileInfo = new FileInfo(deltaPath);

            // Store delta in database
            var deltaId = _database.InsertDeltaFile(screenshot.Id, deltaPath, fileInfo.Length);
            _database.LinkDeltaToScreenshot(screenshot.Id, deltaId);

            // Update screenshot status
            _database.UpdateScreenshotStatus(screenshot.Id, ScreenshotStatus.Optimized, DateTime.UtcNow);

            // Delete original PNG
            File.Delete(screenshot.FilePath);

            _logger.LogDebug("Optimized delta {Id}: {OriginalSize} -> {NewSize}",
                screenshot.Id, screenshot.FileSize, fileInfo.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing delta {Id}", screenshot.Id);
            throw;
        }
    }

    private async Task ProcessOcr(Screenshot screenshot, Bitmap bitmap)
    {
        try
        {
            var languages = _settings.Settings.OcrLanguages;
            if (!languages.Any())
            {
                languages = new List<string> { "en-US" };
            }

            var allText = new List<string>();

            foreach (var language in languages)
            {
                var result = await OcrProcessor.ExtractTextAsync(bitmap, language);
                if (result != null && !string.IsNullOrWhiteSpace(result.Text))
                {
                    allText.Add(result.Text);

                    _database.InsertOcrData(
                        screenshot.Id,
                        result.Text,
                        language,
                        result.Confidence
                    );
                }
            }

            _logger.LogDebug("OCR processed for screenshot {Id}", screenshot.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing OCR for screenshot {Id}", screenshot.Id);
            // OCR is non-critical, so we don't fail the screenshot
        }
    }

    private async Task PerformMaintenance(CancellationToken stoppingToken)
    {
        try
        {
            // Auto-cleanup old screenshots if enabled
            if (_settings.Settings.AutoCleanup)
            {
                await Task.Run(() =>
                {
                    _database.CleanupOldScreenshots(_settings.Settings.RetentionMonths);
                }, stoppingToken);

                _logger.LogInformation("Performed cleanup of old screenshots");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing maintenance");
        }
    }
}
