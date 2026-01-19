using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MinRecall.Core.Database;
using MinRecall.Core.Settings;
using System;
using System.Collections.Generic;
using System.IO;

namespace MinRecall.UI.Pages;

public sealed partial class HeatmapPage : Page
{
    private DatabaseManager? _database;
    private SettingsManager? _settings;
    private List<HeatmapItem> _items = new();

    public HeatmapPage()
    {
        this.InitializeComponent();
        HeatmapItemsControl.ItemsSource = _items;

        var today = DateTimeOffset.Now;
        var startOfMonth = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, today.Offset);
        StartDatePicker.Date = startOfMonth;
        EndDatePicker.Date = today;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MinRecall"
        );

        _database = new DatabaseManager(Path.Combine(appDataPath, "minrecall.db"));
        _settings = new SettingsManager();

        LoadHeatmapData();
    }

    private void DatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        LoadHeatmapData();
    }

    private void LoadHeatmapData()
    {
        if (_database == null) return;

        _items.Clear();

        var startDate = StartDatePicker.Date?.DateTime ?? DateTime.Today.AddDays(-30);
        var endDate = EndDatePicker.Date?.DateTime ?? DateTime.Today;

        var activityData = _database.GetActivityHeatmap(startDate, endDate);

        if (activityData.Count == 0)
        {
            return;
        }

        var totalSeconds = activityData.Values.Sum();
        var maxSeconds = activityData.Values.Max();

        foreach (var kvp in activityData.OrderByDescending(x => x.Value))
        {
            var item = new HeatmapItem
            {
                ProcessName = kvp.Key,
                DurationSeconds = kvp.Value,
                TotalSeconds = totalSeconds,
                MaxSeconds = maxSeconds,
                ScreenshotCount = EstimateScreenshotCount(kvp.Value)
            };

            _items.Add(item);
        }
    }

    private int EstimateScreenshotCount(long durationSeconds)
    {
        // Estimate based on average screenshot interval of 60 seconds
        return Math.Max(1, (int)(durationSeconds / 60));
    }
}

public class HeatmapItem
{
    public string ProcessName { get; set; } = string.Empty;
    public long DurationSeconds { get; set; }
    public long TotalSeconds { get; set; }
    public long MaxSeconds { get; set; }
    public int ScreenshotCount { get; set; }

    public string FormattedDuration => FormatDuration(DurationSeconds);
    public double Percentage => TotalSeconds > 0 ? (double)DurationSeconds / TotalSeconds * 100 : 0;
    public double BarPercentage => MaxSeconds > 0 ? (double)DurationSeconds / MaxSeconds * 100 : 0;

    private string FormatDuration(long seconds)
    {
        var timeSpan = TimeSpan.FromSeconds(seconds);

        if (timeSpan.TotalDays >= 1)
        {
            return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        }
        else if (timeSpan.TotalHours >= 1)
        {
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
        }
        else
        {
            return $"{timeSpan.Minutes}m";
        }
    }
}
