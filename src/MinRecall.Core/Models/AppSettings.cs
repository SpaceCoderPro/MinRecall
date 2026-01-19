namespace MinRecall.Core.Models;

public class AppSettings
{
    // Capture Settings
    public int CaptureIntervalSeconds { get; set; } = 60;
    public int TargetWidth { get; set; } = 1280;
    public int TargetHeight { get; set; } = 720;
    public int JpegQuality { get; set; } = 85;
    public bool AdaptiveQuality { get; set; } = true;

    // Storage Settings
    public int KeyframeFrequency { get; set; } = 25; // Every N screenshots
    public string StorageLocation { get; set; } = string.Empty;
    public int RetentionMonths { get; set; } = 12;

    // Optimization Settings
    public int OptimizationCpuThreshold { get; set; } = 15; // Run when CPU < 15%
    public bool BackgroundOcr { get; set; } = true;
    public bool AutoCleanup { get; set; } = true;

    // Privacy Settings
    public List<string> PrivacyBlacklist { get; set; } = new();
    public bool BlurSensitiveData { get; set; } = false;

    // UI Settings
    public TimelineView TimelineViewPreference { get; set; } = TimelineView.Hourly;
    public ExportFormat DefaultExportFormat { get; set; } = ExportFormat.PNG;
    public bool AutoStartWithWindows { get; set; } = true;

    // Image Quality Profile
    public ImageQualityProfile QualityProfile { get; set; } = ImageQualityProfile.Balanced;
    public List<string> OcrLanguages { get; set; } = new() { "en-US" };

    public AppSettings()
    {
        StorageLocation = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MinRecall",
            "screenshots"
        );
    }
}

public enum TimelineView
{
    Hourly = 0,
    Daily = 1,
    Weekly = 2
}

public enum ExportFormat
{
    PNG = 0,
    JPEG = 1,
    WebP = 2
}

public enum ImageQualityProfile
{
    Storage = 0,
    Balanced = 1,
    Quality = 2
}
