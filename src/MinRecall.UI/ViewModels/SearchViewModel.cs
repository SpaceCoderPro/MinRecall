using CommunityToolkit.Mvvm.ComponentModel;
using MinRecall.Core.Models;
using MinRecall.UI.Services;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MinRecall.UI.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly DatabaseService _database;

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
        _database = DatabaseService.Instance;

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
            var results = _database.SearchScreenshots(SearchQuery, limit: 100);
            
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                SearchResults.Clear();
                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }

                ResultCount = SearchResults.Count;
                HasSearched = true;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            SearchDuration = stopwatch.Elapsed;
            IsSearching = false;
        }
    }
}