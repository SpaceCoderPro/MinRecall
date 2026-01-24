using CommunityToolkit.Mvvm.ComponentModel;
using MinRecall.Core.Models;
using MinRecall.UI.Services;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinRecall.UI.ViewModels;

public partial class TimelineViewModel : ObservableObject
{
    private readonly DatabaseService _database;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private string _viewMode = "daily";

    [ObservableProperty]
    private ObservableCollection<Screenshot> _timelineItems = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public TimelineViewModel()
    {
        _database = DatabaseService.Instance;
        LoadTimelineData();
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        LoadTimelineData();
    }

    partial void OnViewModeChanged(string value)
    {
        LoadTimelineData();
    }

    private async void LoadTimelineData()
    {
        IsLoading = true;
        StatusMessage = "Loading timeline...";
        
        try
        {
            await Task.Run(() =>
            {
                var startOfDay = SelectedDate.Date;
                var endOfDay = startOfDay.AddDays(1).AddSeconds(-1);
                
                var screenshots = _database.GetScreenshots(
                    start: startOfDay,
                    end: endOfDay,
                    processName: null,
                    limit: 500
                );
                
                // Update UI on UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    TimelineItems.Clear();
                    foreach (var item in screenshots)
                    {
                        TimelineItems.Add(item);
                    }
                    StatusMessage = $"Loaded {screenshots.Count} screenshots";
                });
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading timeline: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Error loading timeline data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}