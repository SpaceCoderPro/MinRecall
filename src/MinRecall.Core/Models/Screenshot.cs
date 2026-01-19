namespace MinRecall.Core.Models;

public class Screenshot
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string WindowTitle { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool IsKeyframe { get; set; }
    public long? KeyframeId { get; set; }
    public long? DeltaFileId { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSize { get; set; }
    public ScreenshotStatus Status { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public enum ScreenshotStatus
{
    Pending = 0,
    Optimizing = 1,
    Optimized = 2,
    Failed = 3
}
