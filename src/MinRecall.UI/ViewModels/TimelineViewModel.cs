using CommunityToolkit.Mvvm.ComponentModel;
using MinRecall.Core.Models;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;

namespace MinRecall.UI.ViewModels;

public partial class TimelineViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private string _viewMode = "daily";

    [ObservableProperty]
    private ObservableCollection<Screenshot> _timelineItems = new();

    [ObservableProperty]
    private bool _isLoading;

    public TimelineViewModel()
    {
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
        
        try
        {
            // TODO: Load actual data from MinRecall.Core
            // For now, create sample data
            var sampleData = GenerateSampleTimelineData();
            TimelineItems.Clear();
            
            foreach (var item in sampleData)
            {
                TimelineItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            // TODO: Log error
            System.Diagnostics.Debug.WriteLine($"Error loading timeline data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private IEnumerable<Screenshot> GenerateSampleTimelineData()
    {
        var items = new List<Screenshot>();
        var random = new Random();
        
        for (int i = 0; i < 20; i++)
        {
            var time = SelectedDate.AddHours(random.Next(8, 18)).AddMinutes(random.Next(0, 60));
            items.Add(new Screenshot
            {
                Id = random.Next(1000, 9999),
                Timestamp = time,
                ProcessName = random.Next(2) == 0 ? "VS Code" : "Chrome",
                WindowTitle = GetRandomWindowTitle(),
                FilePath = $"/Assets/Images/sample_{i % 5}.png",
                FileSize = random.Next(500, 2000),
                Width = 1920,
                Height = 1080,
                Status = ScreenshotStatus.Optimized
            });
        }
        
        return items.OrderByDescending(x => x.Timestamp);
    }

    private string GetRandomWindowTitle()
    {
        var titles = new[]
        {
            "Main Program - Development",
            "Dashboard - Analytics",
            "Documentation - API Reference", 
            "Code Review - Pull Request #123",
            "Meeting Notes - Weekly Standup",
            "Database Schema - Users Table",
            "Settings - Application Configuration",
            "Logs - System Messages"
        };
        
        return titles[new Random().Next(titles.Length)];
    }
}