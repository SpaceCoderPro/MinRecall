using CommunityToolkit.Mvvm.ComponentModel;
using MinRecall.Core.Models;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;

namespace MinRecall.UI.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Screenshot> _searchResults = new();

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _hasSearched;

    [ObservableProperty]
    private int _resultCount;

    [ObservableProperty]
    private TimeSpan _searchDuration;

    public SearchViewModel()
    {
        // Subscribe to search query changes
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SearchQuery))
            {
                DebounceSearch();
            }
        };
    }

    private System.Timers.Timer? _searchTimer;

    private void DebounceSearch()
    {
        _searchTimer?.Stop();
        _searchTimer?.Dispose();
        
        _searchTimer = new System.Timers.Timer(500); // 500ms delay
        _searchTimer.Elapsed += (s, e) => PerformSearch();
        _searchTimer.AutoReset = false;
        _searchTimer.Start();
    }

    private async void PerformSearch()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults.Clear();
            HasSearched = false;
            ResultCount = 0;
            return;
        }

        await System.Threading.Tasks.Task.Run(() =>
        {
            DoSearch();
        });
    }

    private void DoSearch()
    {
        IsSearching = true;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // TODO: Implement actual search using MinRecall.Core
            // For now, simulate search with sample data
            var results = GenerateSearchResults();
            
            SearchResults.Clear();
            foreach (var result in results)
            {
                SearchResults.Add(result);
            }

            ResultCount = SearchResults.Count;
            HasSearched = true;
        }
        catch (Exception ex)
        {
            // TODO: Log error
            System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            SearchDuration = stopwatch.Elapsed;
            IsSearching = false;
        }
    }

    private IEnumerable<Screenshot> GenerateSearchResults()
    {
        var allItems = new List<Screenshot>();
        var random = new Random();

        // Generate sample data
        for (int i = 0; i < 50; i++)
        {
            var item = new Screenshot
            {
                Id = random.Next(1000, 9999),
                Timestamp = DateTime.Now.AddHours(-random.Next(0, 72)),
                ProcessName = random.Next(2) == 0 ? "VS Code" : "Chrome",
                WindowTitle = GetRandomWindowTitle(),
                FilePath = $"/Assets/Images/sample_{i % 10}.png",
                FileSize = random.Next(300, 2500),
                Width = 1920,
                Height = 1080,
                Status = ScreenshotStatus.Optimized
            };

            // Filter based on search query
            if (string.IsNullOrWhiteSpace(SearchQuery) ||
                item.ProcessName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                item.WindowTitle.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
            {
                allItems.Add(item);
            }
        }

        return allItems.OrderByDescending(x => x.Timestamp).Take(20);
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
            "Logs - System Messages",
            "Code Editor - Project Alpha",
            "Browser - Stack Overflow"
        };
        
        return titles[new Random().Next(titles.Length)];
    }
}