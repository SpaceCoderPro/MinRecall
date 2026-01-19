namespace MinRecall.Core.Models;

public class ActivityLog
{
    public long Id { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long DurationSeconds { get; set; }
    public int ScreenshotCount { get; set; }
}
