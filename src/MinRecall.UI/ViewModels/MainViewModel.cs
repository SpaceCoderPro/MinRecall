using CommunityToolkit.Mvvm.ComponentModel;
using MinRecall.Core.Models;
using System.Collections.ObjectModel;
using System;

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
        try
        {
            Console.WriteLine("[DEBUG] MainViewModel constructor starting...");
            InitializeViewModels();
            NavigateTo("timeline");
            Console.WriteLine("[DEBUG] MainViewModel constructor completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] MainViewModel constructor failed:");
            Console.WriteLine($"  Type: {ex.GetType().FullName}");
            Console.WriteLine($"  Message: {ex.Message}");
            Console.WriteLine($"  StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    private void InitializeViewModels()
    {
        try
        {
            Console.WriteLine("[DEBUG] InitializeViewModels starting...");
            
            Console.WriteLine("[DEBUG] Creating TimelineViewModel...");
            _viewModels["timeline"] = new TimelineViewModel();
            
            Console.WriteLine("[DEBUG] Creating SearchViewModel...");
            _viewModels["search"] = new SearchViewModel();
            
            Console.WriteLine("[DEBUG] Creating SettingsViewModel...");
            _viewModels["settings"] = new SettingsViewModel();
            
            Console.WriteLine("[DEBUG] All ViewModels initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] InitializeViewModels failed:");
            Console.WriteLine($"  Type: {ex.GetType().FullName}");
            Console.WriteLine($"  Message: {ex.Message}");
            Console.WriteLine($"  StackTrace: {ex.StackTrace}");
            throw;
        }
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
                "settings" => "Settings - MinRecall",
                _ => "MinRecall"
            };
        }
    }
}