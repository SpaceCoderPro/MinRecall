using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MinRecall.UI.ViewModels;

public partial class HeatmapViewModel : ObservableObject
{
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
            // TODO: Load actual data from MinRecall.Core
            var (hourlyData, topApplications, totalScreenshots, mostActiveHour) = GenerateSampleHeatmapData();
            HourlyData.Clear();
            TopApplications.Clear();
            
            foreach (var item in hourlyData)
            {
                HourlyData.Add(item);
            }
            
            foreach (var app in topApplications)
            {
                TopApplications.Add(app);
            }

            TotalScreenshots = totalScreenshots;
            MostActiveHour = mostActiveHour;
        }
        catch (Exception ex)
        {
            // TODO: Log error
            System.Diagnostics.Debug.WriteLine($"Error loading heatmap data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private (List<ActivityData> hourlyData, List<ApplicationUsage> topApplications, int totalScreenshots, int mostActiveHour) GenerateSampleHeatmapData()
    {
        var hourlyData = new List<ActivityData>();
        var topApps = new List<ApplicationUsage>();
        var random = new Random();
        int totalScreenshots = 0;
        int mostActiveHour = 9;
        int maxActivity = 0;

        // Generate hourly data (0-23 hours)
        for (int hour = 0; hour < 24; hour++)
        {
            var activity = random.Next(0, 20);
            totalScreenshots += activity;
            
            if (activity > maxActivity)
            {
                maxActivity = activity;
                mostActiveHour = hour;
            }
            
            hourlyData.Add(new ActivityData
            {
                Hour = hour,
                Activity = activity,
                Percentage = (activity / 20.0) * 100
            });
        }

        // Generate top applications
        var applications = new[]
        {
            ("VS Code", 25, "#0078d4"),
            ("Chrome", 30, "#4285f4"),
            ("Slack", 15, "#4a154b"),
            ("Teams", 12, "#6264a7"),
            ("Explorer", 8, "#0078d4"),
            ("Notepad", 5, "#ff8c00"),
            ("Paint", 3, "#00bcf2"),
            ("Calculator", 2, "#0078d4")
        };

        foreach (var (name, percentage, color) in applications)
        {
            topApps.Add(new ApplicationUsage
            {
                ApplicationName = name,
                Percentage = percentage,
                Color = color,
                ScreenshotCount = percentage * 12 // Rough calculation
            });
        }

        return (hourlyData, topApps, totalScreenshots, mostActiveHour);
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