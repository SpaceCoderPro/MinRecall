using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MinRecall.UI.ViewModels;

public partial class ActivityViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<ActivityLogItem> _activityLog = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableDates = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalActivities;

    [ObservableProperty]
    private TimeSpan _totalActiveTime;

    [ObservableProperty]
    private string _mostUsedApplication = string.Empty;

    public ActivityViewModel()
    {
        LoadActivityData();
        LoadAvailableDates();
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        LoadActivityData();
    }

    private void LoadActivityData()
    {
        IsLoading = true;
        
        try
        {
            var sampleData = GenerateSampleActivityData();
            ActivityLog.Clear();
            
            foreach (var item in sampleData)
            {
                ActivityLog.Add(item);
            }

            TotalActivities = ActivityLog.Count;
            TotalActiveTime = CalculateTotalActiveTime();
            MostUsedApplication = FindMostUsedApplication();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading activity data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadAvailableDates()
    {
        var dates = new List<DateTime>();
        var today = DateTime.Today;
        
        for (int i = 0; i < 30; i++)
        {
            dates.Add(today.AddDays(-i));
        }
        
        AvailableDates.Clear();
        foreach (var date in dates)
        {
            AvailableDates.Add(date.ToString("yyyy-MM-dd"));
        }
    }

    private IEnumerable<ActivityLogItem> GenerateSampleActivityData()
    {
        var activities = new List<ActivityLogItem>();
        var random = new Random();
        var currentDate = SelectedDate;
        
        var startTime = currentDate.Date.AddHours(8);
        var endTime = currentDate.Date.AddHours(20);
        
        var currentTime = startTime;
        var currentProcess = string.Empty;
        
        while (currentTime < endTime)
        {
            var duration = TimeSpan.FromMinutes(random.Next(15, 91));
            var nextActivity = random.Next(3) == 0;
            
            if (nextActivity || string.IsNullOrEmpty(currentProcess))
            {
                currentProcess = GetRandomProcess();
                activities.Add(new ActivityLogItem
                {
                    Id = Guid.NewGuid(),
                    StartTime = currentTime,
                    EndTime = currentTime.Add(duration),
                    ProcessName = currentProcess,
                    WindowTitle = GetRandomWindowTitle(currentProcess),
                    ScreenshotCount = random.Next(1, 15)
                });
            }
            
            currentTime = currentTime.Add(duration);
        }

        return activities.OrderBy(x => x.StartTime);
    }

    private string GetRandomProcess()
    {
        var processes = new[]
        {
            "VS Code", "Chrome", "Slack", "Teams", "Explorer", 
            "Notepad", "Paint", "Calculator", "Word", "Excel",
            "PowerPoint", "Outlook", "Spotify", "Discord", "GitHub Desktop"
        };
        
        return processes[new Random().Next(processes.Length)];
    }

    private string GetRandomWindowTitle(string processName)
    {
        var titles = new Dictionary<string, string[]>
        {
            ["VS Code"] = new[] { "Main Program - Development", "Code Review - PR #123", "API Documentation", "Project Settings" },
            ["Chrome"] = new[] { "Google Search", "Stack Overflow", "GitHub - Repository", "Documentation" },
            ["Slack"] = new[] { "General Channel", "Development Team", "Random Chat", "Project Updates" },
            ["Teams"] = new[] { "Weekly Standup", "Code Review", "Project Planning", "Team Chat" },
            ["Explorer"] = new[] { "Downloads", "Documents", "Project Folder", "System Drive" },
            ["Notepad"] = new[] { "Meeting Notes", "TODO List", "Ideas", "Draft Document" }
        };

        if (titles.TryGetValue(processName, out var processTitles))
        {
            return processTitles[new Random().Next(processTitles.Length)];
        }
        
        return $"{processName} - Window";
    }

    private TimeSpan CalculateTotalActiveTime()
    {
        var total = TimeSpan.Zero;
        foreach (var activity in ActivityLog)
        {
            total += activity.EndTime - activity.StartTime;
        }
        return total;
    }

    private string FindMostUsedApplication()
    {
        var processUsage = new Dictionary<string, TimeSpan>();
        
        foreach (var activity in ActivityLog)
        {
            var duration = activity.EndTime - activity.StartTime;
            if (processUsage.ContainsKey(activity.ProcessName))
            {
                processUsage[activity.ProcessName] += duration;
            }
            else
            {
                processUsage[activity.ProcessName] = duration;
            }
        }
        
        return processUsage.OrderByDescending(x => x.Value).FirstOrDefault().Key ?? "Unknown";
    }
}

public class ActivityLogItem
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public int ScreenshotCount { get; set; }
}