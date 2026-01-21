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

    private void NavItem_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        // Simple navigation - for now just handle the pointer press
        System.Diagnostics.Debug.WriteLine($"Navigation item tapped: {sender?.GetType().Name}");
    }
}