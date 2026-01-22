using CommunityToolkit.Mvvm.ComponentModel;
using MinRecall.UI.Services;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinRecall.UI.ViewModels;

public partial class ActivityViewModel : ObservableObject
{
    private readonly DatabaseService _database;

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
        _database = DatabaseService.Instance;
        LoadActivityData();
        LoadAvailableDates();
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        LoadActivityData();
    }

    private async void LoadActivityData()
    {
        IsLoading = true;
        
        try
        {
            await Task.Run(() =>
            {
                var startOfDay = SelectedDate.Date;
                var endOfDay = startOfDay.AddDays(1).AddSeconds(-1);
                
                var activityLogs = _database.GetActivityLog(startOfDay, endOfDay);
                
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ActivityLog.Clear();
                    foreach (var log in activityLogs)
                    {
                        ActivityLog.Add(new ActivityLogItem
                        {
                            Id = Guid.NewGuid(),
                            StartTime = log.StartTime,
                            EndTime = log.EndTime,
                            ProcessName = log.ProcessName,
                            WindowTitle = log.WindowTitle,
                            ScreenshotCount = log.ScreenshotCount
                        });
                    }

                    TotalActivities = ActivityLog.Count;
                    TotalActiveTime = CalculateTotalActiveTime();
                    MostUsedApplication = FindMostUsedApplication();
                });
            });
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