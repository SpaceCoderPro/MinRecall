using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System;

namespace MinRecall.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _captureInterval = "5";

    [ObservableProperty]
    private int _imageQuality = 85;

    [ObservableProperty]
    private bool _enableCompression = true;

    [ObservableProperty]
    private string _compressionType = "AVIF";

    [ObservableProperty]
    private bool _enableOcr = true;

    [ObservableProperty]
    private bool _enablePrivacyBlacklist = true;

    [ObservableProperty]
    private ObservableCollection<string> _blacklistedApps = new()
    {
        "Password Manager", "Banking App", "System Settings"
    };

    [ObservableProperty]
    private bool _enableNotifications = true;

    [ObservableProperty]
    private bool _startWithWindows = false;

    [ObservableProperty]
    private int _maxStorageDays = 30;

    [ObservableProperty]
    private string _theme = "Dark";

    [ObservableProperty]
    private bool _enableAnimations = true;

    [ObservableProperty]
    private bool _autoStartCapture = true;

    [ObservableProperty]
    private int _cpuThreshold = 70;

    public SettingsViewModel()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        // TODO: Load actual settings from MinRecall.Core
        // For now, using default values
    }

    public void SaveSettings()
    {
        // TODO: Save settings to MinRecall.Core
        System.Diagnostics.Debug.WriteLine("Settings saved successfully");
    }

    partial void OnCaptureIntervalChanged(string value)
    {
        if (int.TryParse(value, out var interval) && interval >= 1 && interval <= 60)
        {
            // Valid interval
        }
        else
        {
            CaptureInterval = "5"; // Reset to default
        }
    }

    partial void OnImageQualityChanged(int value)
    {
        if (value < 10 || value > 100)
        {
            ImageQuality = 85; // Reset to default
        }
    }

    partial void OnMaxStorageDaysChanged(int value)
    {
        if (value < 1 || value > 365)
        {
            MaxStorageDays = 30; // Reset to default
        }
    }

    partial void OnCpuThresholdChanged(int value)
    {
        if (value < 10 || value > 100)
        {
            CpuThreshold = 70; // Reset to default
        }
    }
}