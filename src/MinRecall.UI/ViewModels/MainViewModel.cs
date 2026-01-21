using CommunityToolkit.Mvvm.ComponentModel;
using MinRecall.Core.Models;
using System.Collections.ObjectModel;

namespace MinRecall.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private string _title = "MinRecall - AI-Powered Screenshot Recall";

    private readonly Dictionary<string, object> _viewModels = new();

    public MainViewModel()
    {
        InitializeViewModels();
        NavigateTo("timeline");
    }

    private void InitializeViewModels()
    {
        _viewModels["timeline"] = new TimelineViewModel();
        _viewModels["search"] = new SearchViewModel();
        _viewModels["heatmap"] = new HeatmapViewModel();
        _viewModels["activity"] = new ActivityViewModel();
        _viewModels["settings"] = new SettingsViewModel();
    }

    public void NavigateTo(string viewName)
    {
        if (_viewModels.TryGetValue(viewName, out var viewModel))
        {
            CurrentViewModel = viewModel;
            Title = viewName switch
            {
                "timeline" => "Timeline - MinRecall",
                "search" => "Search - MinRecall", 
                "heatmap" => "Activity Heatmap - MinRecall",
                "activity" => "Activity Log - MinRecall",
                "settings" => "Settings - MinRecall",
                _ => "MinRecall"
            };
        }
    }
}