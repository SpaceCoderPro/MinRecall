using CommunityToolkit.Mvvm.ComponentModel;
using MinRecall.UI.Services;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinRecall.UI.ViewModels;

public partial class HeatmapViewModel : ObservableObject
{
    private readonly DatabaseService _database;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<ActivityData> _hourlyData = new();

    [ObservableProperty]
    private ObservableCollection<ApplicationUsage> _topApplications = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalScreenshots;

    [ObservableProperty]
    private int _mostActiveHour;

    public HeatmapViewModel()
    {
        _database = DatabaseService.Instance;
        LoadHeatmapData();
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        LoadHeatmapData();
    }

    private void LoadHeatmapData()
    {
        IsLoading = true;
        
        try
        {
            var startOfDay = SelectedDate.Date;
            var endOfDay = startOfDay.AddDays(1).AddSeconds(-1);
            
            // Get activity heatmap data
            var heatmap = _database.GetActivityHeatmap(startOfDay, endOfDay);
            
            // Get all screenshots for the day
            var screenshots = _database.GetScreenshots(startOfDay, endOfDay, null, 1000);
            
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // Process hourly data
                var hourlyData = ProcessHourlyData(screenshots);
                HourlyData.Clear();
                foreach (var item in hourlyData)
                {
                    HourlyData.Add(item);
                }
                
                // Process top applications
                var topApps = ProcessTopApplications(heatmap);
                TopApplications.Clear();
                foreach (var app in topApps)
                {
                    TopApplications.Add(app);
                }

                TotalScreenshots = screenshots.Count;
                MostActiveHour = hourlyData.OrderByDescending(h => h.Activity).FirstOrDefault()?.Hour ?? 0;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading heatmap data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private List<ActivityData> ProcessHourlyData(List<MinRecall.Core.Models.Screenshot> screenshots)
    {
        var hourlyData = new List<ActivityData>();
        var maxActivity = 0;

        for (int hour = 0; hour < 24; hour++)
        {
            var hourStart = SelectedDate.Date.AddHours(hour);
            var hourEnd = hourStart.AddHours(1);
            
            var activity = screenshots.Count(s => s.Timestamp >= hourStart && s.Timestamp < hourEnd);
            if (activity > maxActivity) maxActivity = activity;

            hourlyData.Add(new ActivityData
            {
                Hour = hour,
                Activity = activity,
                Percentage = 0
            });
        }

        // Calculate percentages
        if (maxActivity > 0)
        {
            foreach (var data in hourlyData)
            {
                data.Percentage = (data.Activity / (double)maxActivity) * 100;
            }
        }

        return hourlyData;
    }

    private List<ApplicationUsage> ProcessTopApplications(Dictionary<string, long> heatmap)
    {
        var topApps = new List<ApplicationUsage>();
        var totalSeconds = heatmap.Values.Sum();

        var colors = new[] { "#0078d4", "#4285f4", "#4a154b", "#6264a7", "#ff8c00", "#00bcf2", "#e74856", "#00b7c3" };
        int colorIndex = 0;

        foreach (var (process, seconds) in heatmap.OrderByDescending(kv => kv.Value).Take(8))
        {
            var percentage = totalSeconds > 0 ? (int)((seconds * 100) / totalSeconds) : 0;
            topApps.Add(new ApplicationUsage
            {
                ApplicationName = process,
                Percentage = percentage,
                Color = colors[colorIndex % colors.Length],
                ScreenshotCount = percentage * 12
            });
            colorIndex++;
        }

        return topApps;
    }
}

public class ActivityData
{
    public int Hour { get; set; }
    public int Activity { get; set; }
    public double Percentage { get; set; }
}

public class ApplicationUsage
{
    public string ApplicationName { get; set; } = string.Empty;
    public int Percentage { get; set; }
    public string Color { get; set; } = string.Empty;
    public int ScreenshotCount { get; set; }
}