using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;

namespace MinRecall.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void NavItem_Tapped(object? sender, RoutedEventArgs e)
    {
        // Simple navigation - for now just change the content
        if (sender is StackPanel navItem && navItem.Tag is string tag)
        {
            // Update active states
            UpdateNavigationStates(tag);
        }
    }

    private void UpdateNavigationStates(string activeTag)
    {
        // Reset all navigation items
        if (FindControl<StackPanel>("NavTimeline") is var navTimeline)
            navTimeline.Classes.Set("active", activeTag == "timeline");
        if (FindControl<StackPanel>("NavSearch") is var navSearch)
            navSearch.Classes.Set("active", activeTag == "search");
        if (FindControl<StackPanel>("NavHeatmap") is var navHeatmap)
            navHeatmap.Classes.Set("active", activeTag == "heatmap");
        if (FindControl<StackPanel>("NavActivity") is var navActivity)
            navActivity.Classes.Set("active", activeTag == "activity");
        if (FindControl<StackPanel>("NavSettings") is var navSettings)
            navSettings.Classes.Set("active", activeTag == "settings");
    }
}