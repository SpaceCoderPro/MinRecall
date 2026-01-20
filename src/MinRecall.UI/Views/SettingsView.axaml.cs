using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinRecall.UI.ViewModels;

namespace MinRecall.UI.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        
        DataContext = new SettingsViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void DecreaseInterval(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && int.TryParse(vm.CaptureInterval, out var interval))
        {
            vm.CaptureInterval = Math.Max(1, interval - 1).ToString();
        }
    }

    private void IncreaseInterval(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && int.TryParse(vm.CaptureInterval, out var interval))
        {
            vm.CaptureInterval = Math.Min(60, interval + 1).ToString();
        }
    }

    private void SaveSettings(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.SaveSettings();
            
            // Show success notification (could be improved with a proper notification system)
            System.Diagnostics.Debug.WriteLine("Settings saved successfully!");
        }
    }

    private void ResetToDefaults(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.CaptureInterval = "5";
            vm.ImageQuality = 85;
            vm.EnableCompression = true;
            vm.CompressionType = "AVIF";
            vm.EnableOcr = true;
            vm.EnablePrivacyBlacklist = true;
            vm.EnableNotifications = true;
            vm.StartWithWindows = false;
            vm.MaxStorageDays = 30;
            vm.Theme = "Dark";
            vm.EnableAnimations = true;
            vm.AutoStartCapture = true;
            vm.CpuThreshold = 70;
            
            System.Diagnostics.Debug.WriteLine("Settings reset to defaults!");
        }
    }
}