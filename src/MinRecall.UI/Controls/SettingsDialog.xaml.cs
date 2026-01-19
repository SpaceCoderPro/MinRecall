using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MinRecall.Core.Models;
using MinRecall.Core.Settings;
using System;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MinRecall.UI.Controls;

public sealed partial class SettingsDialog : ContentDialog
{
    private SettingsManager? _settings;
    private string _storageLocation = string.Empty;

    public SettingsDialog()
    {
        this.InitializeComponent();
        this.Loaded += SettingsDialog_Loaded;
        this.PrimaryButtonClick += SettingsDialog_PrimaryButtonClick;
    }

    private void SettingsDialog_Loaded(object sender, RoutedEventArgs e)
    {
        _settings = new SettingsManager();
        LoadSettings();
    }

    private void LoadSettings()
    {
        if (_settings == null) return;

        var settings = _settings.Settings;

        CaptureIntervalBox.Value = settings.CaptureIntervalSeconds;
        QualityProfileBox.SelectedIndex = (int)settings.QualityProfile;
        AdaptiveQualityToggle.IsOn = settings.AdaptiveQuality;
        KeyframeFrequencyBox.Value = settings.KeyframeFrequency;
        CpuThresholdBox.Value = settings.OptimizationCpuThreshold;
        BackgroundOcrToggle.IsOn = settings.BackgroundOcr;
        AutoCleanupToggle.IsOn = settings.AutoCleanup;
        RetentionMonthsBox.Value = settings.RetentionMonths;
        BlurSensitiveToggle.IsOn = settings.BlurSensitiveData;
        AutoStartToggle.IsOn = settings.AutoStartWithWindows;

        _storageLocation = settings.StorageLocation;
        StorageLocationText.Text = _storageLocation;

        PrivacyBlacklistBox.Text = string.Join("\n", settings.PrivacyBlacklist);
        OcrLanguagesBox.Text = string.Join(", ", settings.OcrLanguages);

        TimelineViewBox.SelectedIndex = (int)settings.TimelineViewPreference;
        ExportFormatBox.SelectedIndex = (int)settings.DefaultExportFormat;
    }

    private void QualityProfileBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_settings == null || QualityProfileBox.SelectedItem == null) return;

        var selectedItem = QualityProfileBox.SelectedItem as ComboBoxItem;
        var profile = selectedItem?.Tag?.ToString() switch
        {
            "Storage" => ImageQualityProfile.Storage,
            "Balanced" => ImageQualityProfile.Balanced,
            "Quality" => ImageQualityProfile.Quality,
            _ => ImageQualityProfile.Balanced
        };

        _settings.UpdateQualityProfile(profile);
    }

    private async void BrowseLocationButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new Window();
        var hwnd = WindowNative.GetWindowHandle(window);
        var picker = new FolderPicker();

        InitializeWithWindow.Initialize(picker, hwnd);

        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add("*");

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            _storageLocation = folder.Path;
            StorageLocationText.Text = _storageLocation;
        }
    }

    private void SettingsDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_settings == null) return;

        var settings = _settings.Settings;

        settings.CaptureIntervalSeconds = (int)CaptureIntervalBox.Value;
        settings.AdaptiveQuality = AdaptiveQualityToggle.IsOn;
        settings.KeyframeFrequency = (int)KeyframeFrequencyBox.Value;
        settings.OptimizationCpuThreshold = (int)CpuThresholdBox.Value;
        settings.BackgroundOcr = BackgroundOcrToggle.IsOn;
        settings.AutoCleanup = AutoCleanupToggle.IsOn;
        settings.RetentionMonths = (int)RetentionMonthsBox.Value;
        settings.BlurSensitiveData = BlurSensitiveToggle.IsOn;
        settings.AutoStartWithWindows = AutoStartToggle.IsOn;
        settings.StorageLocation = _storageLocation;

        // Parse privacy blacklist
        var blacklist = PrivacyBlacklistBox.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
        settings.PrivacyBlacklist = blacklist;

        // Parse OCR languages
        var languages = OcrLanguagesBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
        settings.OcrLanguages = languages.Any() ? languages : new List<string> { "en-US" };

        // Set timeline view
        var timelineItem = TimelineViewBox.SelectedItem as ComboBoxItem;
        settings.TimelineViewPreference = timelineItem?.Tag?.ToString() switch
        {
            "Hourly" => TimelineView.Hourly,
            "Daily" => TimelineView.Daily,
            "Weekly" => TimelineView.Weekly,
            _ => TimelineView.Daily
        };

        // Set export format
        var exportItem = ExportFormatBox.SelectedItem as ComboBoxItem;
        settings.DefaultExportFormat = exportItem?.Tag?.ToString() switch
        {
            "PNG" => ExportFormat.PNG,
            "JPEG" => ExportFormat.JPEG,
            "WebP" => ExportFormat.WebP,
            _ => ExportFormat.PNG
        };

        _settings.Save();
    }
}
