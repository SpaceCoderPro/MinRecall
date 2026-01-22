using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MinRecall.UI.ViewModels;
using System;

namespace MinRecall.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private Border? _contentFrame;
    private StackPanel? _navTimeline;
    private StackPanel? _navSearch;
    private StackPanel? _navHeatmap;
    private StackPanel? _navActivity;
    private StackPanel? _navSettings;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        
        // Cache controls for navigation
        _contentFrame = this.FindControl<Border>("ContentFrame");
        _navTimeline = this.FindControl<StackPanel>("NavTimeline");
        _navSearch = this.FindControl<StackPanel>("NavSearch");
        _navHeatmap = this.FindControl<StackPanel>("NavHeatmap");
        _navActivity = this.FindControl<StackPanel>("NavActivity");
        _navSettings = this.FindControl<StackPanel>("NavSettings");

        // Show timeline view by default
        NavigateTo("timeline");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void NavItem_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is StackPanel navItem)
        {
            var tag = navItem.Tag?.ToString();
            if (!string.IsNullOrEmpty(tag))
            {
                NavigateTo(tag);
            }
        }
    }

    private void NavigateTo(string viewName)
    {
        // Update ViewModel
        _viewModel.NavigateTo(viewName);

        // Update UI - remove active class from all nav items
        _navTimeline?.Classes.Remove("active");
        _navSearch?.Classes.Remove("active");
        _navHeatmap?.Classes.Remove("active");
        _navActivity?.Classes.Remove("active");
        _navSettings?.Classes.Remove("active");

        // Add active class to selected nav item
        var activeNav = viewName switch
        {
            "timeline" => _navTimeline,
            "search" => _navSearch,
            "heatmap" => _navHeatmap,
            "activity" => _navActivity,
            "settings" => _navSettings,
            _ => null
        };
        activeNav?.Classes.Add("active");

        // Update content frame
        if (_contentFrame != null)
        {
            _contentFrame.Child = viewName switch
            {
                "timeline" => new TimelineView { DataContext = _viewModel.CurrentViewModel },
                "search" => new SearchView { DataContext = _viewModel.CurrentViewModel },
                "heatmap" => new HeatmapView { DataContext = _viewModel.CurrentViewModel },
                "activity" => new ActivityView { DataContext = _viewModel.CurrentViewModel },
                "settings" => new SettingsView { DataContext = _viewModel.CurrentViewModel },
                _ => null
            };
        }
    }
}