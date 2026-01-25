using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MinRecall.UI.ViewModels;
using System;

namespace MinRecall.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel? _viewModel;
    private Border? _contentFrame;
    private StackPanel? _navTimeline;
    private StackPanel? _navSearch;
    private StackPanel? _navHeatmap;
    private StackPanel? _navActivity;
    private StackPanel? _navSettings;

    public MainWindow()
    {
        try
        {
            Console.WriteLine("[DEBUG] MainWindow constructor starting...");
            
            InitializeComponent();
            Console.WriteLine("[DEBUG] InitializeComponent completed");
            
            try
            {
                Console.WriteLine("[DEBUG] Creating MainViewModel...");
                _viewModel = new MainViewModel();
                DataContext = _viewModel;
                Console.WriteLine("[DEBUG] MainViewModel created and set as DataContext");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to create MainViewModel:");
                Console.WriteLine($"  Type: {ex.GetType().FullName}");
                Console.WriteLine($"  Message: {ex.Message}");
                Console.WriteLine($"  StackTrace: {ex.StackTrace}");
                
                // Show error in window title
                Title = "MinRecall - Error: Failed to initialize";
                
                // Continue anyway - show the window even if ViewModel fails
            }
            
            // Cache controls for navigation
            try
            {
                Console.WriteLine("[DEBUG] Caching navigation controls...");
                _contentFrame = this.FindControl<Border>("ContentFrame");
                _navTimeline = this.FindControl<StackPanel>("NavTimeline");
                _navSearch = this.FindControl<StackPanel>("NavSearch");
                _navHeatmap = this.FindControl<StackPanel>("NavHeatmap");
                _navActivity = this.FindControl<StackPanel>("NavActivity");
                _navSettings = this.FindControl<StackPanel>("NavSettings");
                Console.WriteLine("[DEBUG] Navigation controls cached");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Failed to cache navigation controls: {ex.Message}");
            }

            // Show timeline view by default
            try
            {
                Console.WriteLine("[DEBUG] Navigating to timeline view...");
                NavigateTo("timeline");
                Console.WriteLine("[DEBUG] Navigation to timeline completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to navigate to timeline:");
                Console.WriteLine($"  Message: {ex.Message}");
                Console.WriteLine($"  StackTrace: {ex.StackTrace}");
            }
            
            Console.WriteLine("[DEBUG] MainWindow constructor completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FATAL] MainWindow constructor failed:");
            Console.WriteLine($"  Type: {ex.GetType().FullName}");
            Console.WriteLine($"  Message: {ex.Message}");
            Console.WriteLine($"  StackTrace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[FATAL] Inner Exception:");
                Console.WriteLine($"  Type: {ex.InnerException.GetType().FullName}");
                Console.WriteLine($"  Message: {ex.InnerException.Message}");
            }
            
            throw;
        }
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
        try
        {
            Console.WriteLine($"[DEBUG] NavigateTo({viewName}) starting...");
            
            // Update ViewModel
            if (_viewModel != null)
            {
                try
                {
                    Console.WriteLine($"[DEBUG] Calling _viewModel.NavigateTo({viewName})...");
                    _viewModel.NavigateTo(viewName);
                    Console.WriteLine($"[DEBUG] _viewModel.NavigateTo({viewName}) completed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] _viewModel.NavigateTo failed: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("[WARN] _viewModel is null, skipping NavigateTo call");
            }

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
            if (_contentFrame != null && _viewModel != null)
            {
                try
                {
                    Console.WriteLine($"[DEBUG] Creating view for {viewName}...");
                    _contentFrame.Child = viewName switch
                    {
                        "timeline" => new TimelineView { DataContext = _viewModel.CurrentViewModel },
                        "search" => new SearchView { DataContext = _viewModel.CurrentViewModel },
                        "heatmap" => new HeatmapView { DataContext = _viewModel.CurrentViewModel },
                        "activity" => new ActivityView { DataContext = _viewModel.CurrentViewModel },
                        "settings" => new SettingsView { DataContext = _viewModel.CurrentViewModel },
                        _ => null
                    };
                    Console.WriteLine($"[DEBUG] View created for {viewName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to create view for {viewName}:");
                    Console.WriteLine($"  Message: {ex.Message}");
                    Console.WriteLine($"  StackTrace: {ex.StackTrace}");
                    
                    // Show error in content frame
                    var errorText = new TextBlock
                    {
                        Text = $"Error loading {viewName} view:\n{ex.Message}",
                        Foreground = Avalonia.Media.Brushes.Red,
                        Margin = new Thickness(20)
                    };
                    _contentFrame.Child = errorText;
                }
            }
            
            Console.WriteLine($"[DEBUG] NavigateTo({viewName}) completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] NavigateTo failed:");
            Console.WriteLine($"  Message: {ex.Message}");
            Console.WriteLine($"  StackTrace: {ex.StackTrace}");
        }
    }
}