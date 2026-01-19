using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MinRecall.Core.Database;
using MinRecall.Core.Models;
using MinRecall.Core.Settings;
using System;
using System.Collections.Generic;
using System.IO;

namespace MinRecall.UI.Pages;

public sealed partial class ActivityPage : Page
{
    private DatabaseManager? _database;
    private SettingsManager? _settings;
    private List<ActivityLogItem> _items = new();
    private List<string> _uniqueApps = new();

    public ActivityPage()
    {
        this.InitializeComponent();
        ActivityItemsControl.ItemsSource = _items;

        var today = DateTimeOffset.Now;
        var startOfMonth = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, today.Offset);
        StartDatePicker.Date = startOfMonth;
        EndDatePicker.Date = today;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MinRecall"
        );

        _database = new DatabaseManager(Path.Combine(appDataPath, "minrecall.db"));
        _settings = new SettingsManager();

        LoadActivityData();
    }

    private void DatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        LoadActivityData();
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadActivityData();
    }

    private void LoadActivityData()
    {
        if (_database == null) return;

        _items.Clear();

        var startDate = StartDatePicker.Date?.DateTime ?? DateTime.Today.AddDays(-7);
        var endDate = EndDatePicker.Date?.DateTime ?? DateTime.Today;

        var selectedItem = FilterComboBox.SelectedItem as ComboBoxItem;
        var filterProcess = selectedItem?.Tag?.ToString() ?? "";

        var activityLogs = _database.GetActivityLog(startDate, endDate,
            string.IsNullOrEmpty(filterProcess) ? null : filterProcess);

        // Populate filter combo if empty
        if (FilterComboBox.Items.Count <= 1)
        {
            _uniqueApps = activityLogs.Select(l => l.ProcessName).Distinct().ToList();
            foreach (var app in _uniqueApps)
            {
                FilterComboBox.Items.Add(new ComboBoxItem { Tag = app, Content = app });
            }
        }

        foreach (var log in activityLogs.OrderByDescending(l => l.StartTime))
        {
            var item = new ActivityLogItem
            {
                ProcessName = log.ProcessName,
                WindowTitle = log.WindowTitle,
                StartTime = log.StartTime,
                EndTime = log.EndTime,
                DurationSeconds = log.DurationSeconds,
                ScreenshotCount = log.ScreenshotCount
            };

            _items.Add(item);
        }
    }
}

public class ActivityLogItem
{
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long DurationSeconds { get; set; }
    public int ScreenshotCount { get; set; }

    public string FirstLetter => string.IsNullOrEmpty(ProcessName) ? "?" :
        char.ToUpper(ProcessName[0]).ToString();

    public string FormattedDuration => FormatDuration(DurationSeconds);
    public string TimeRange => $"{StartTime:MMM dd, HH:mm} - {EndTime:HH:mm}";

    private string FormatDuration(long seconds)
    {
        var timeSpan = TimeSpan.FromSeconds(seconds);

        if (timeSpan.TotalHours >= 1)
        {
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
        }
        else
        {
            return $"{timeSpan.Minutes}m";
        }
    }
}
