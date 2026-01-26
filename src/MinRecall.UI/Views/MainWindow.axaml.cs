using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MinRecall.UI.ViewModels;
using System;

namespace MinRecall.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel? _viewModel;
    private Border? _contentFrame;
    private Button? _navTimeline;
    private Button? _navSearch;
    private Button? _navSettings;
    private TextBlock? _pageTitle;
    private TextBox? _searchBox;

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
                
                Title = "MinRecall - Error: Failed to initialize";
            }
            
            try
            {
                Console.WriteLine("[DEBUG] Caching navigation controls...");
                _contentFrame = this.FindControl<Border>("ContentFrame");
                _navTimeline = this.FindControl<Button>("NavTimeline");
                _navSearch = this.FindControl<Button>("NavSearch");
                _navSettings = this.FindControl<Button>("NavSettings");
                _pageTitle = this.FindControl<TextBlock>("PageTitle");
                _searchBox = this.FindControl<TextBox>("SearchBox");
                Console.WriteLine("[DEBUG] Navigation controls cached");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Failed to cache navigation controls: {ex.Message}");
            }

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

    private void NavItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _searchBox?.Text is string query && !string.IsNullOrWhiteSpace(query))
        {
            NavigateTo("search");
            // The search view will handle the query
        }
    }

    private void Settings_Click(object? sender, RoutedEventArgs e)
    {
        NavigateTo("settings");
    }

    private void NavigateTo(string viewName)
    {
        try
        {
            Console.WriteLine($"[DEBUG] NavigateTo({viewName}) starting...");
            
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

            // Update navigation button states
            if (_navTimeline != null) _navTimeline.Classes.Remove("active");
            if (_navSearch != null) _navSearch.Classes.Remove("active");
            if (_navSettings != null) _navSettings.Classes.Remove("active");

            var activeNav = viewName switch
            {
                "timeline" => _navTimeline,
                "search" => _navSearch,
                "settings" => _navSettings,
                _ => null
            };
            activeNav?.Classes.Add("active");

            // Update page title
            if (_pageTitle != null)
            {
                _pageTitle.Text = viewName switch
                {
                    "timeline" => "Timeline",
                    "search" => "Search",
                    "settings" => "Settings",
                    _ => "MinRecall"
                };
            }

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
