using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using MinRecall.Core.Database;
using MinRecall.Core.Models;
using MinRecall.Core.Settings;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace MinRecall.UI.Pages;

public sealed partial class TimelinePage : Page
{
    private DatabaseManager? _database;
    private SettingsManager? _settings;
    private ObservableCollection<TimelineItem> _items = new();

    public TimelinePage()
    {
        this.InitializeComponent();
        TimelineItemsControl.ItemsSource = _items;
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

        DatePicker.Date = DateTimeOffset.Now;
        LoadScreenshots();
    }

    private void DatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (args.NewDate.HasValue)
        {
            LoadScreenshots();
        }
    }

    private void ViewModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadScreenshots();
    }

    private void LoadScreenshots()
    {
        if (_database == null) return;

        _items.Clear();

        var selectedDate = DatePicker.Date?.DateTime ?? DateTime.Now;
        DateTime start, end;

        var viewMode = ViewModeComboBox.SelectedItem as ComboBoxItem;
        var mode = viewMode?.Tag?.ToString() ?? "daily";

        switch (mode)
        {
            case "hourly":
                start = selectedDate.Date;
                end = start.AddHours(1);
                break;
            case "daily":
                start = selectedDate.Date;
                end = start.AddDays(1);
                break;
            case "weekly":
                start = selectedDate.Date.AddDays(-(int)selectedDate.DayOfWeek);
                end = start.AddDays(7);
                break;
            default:
                start = selectedDate.Date;
                end = start.AddDays(1);
                break;
        }

        var screenshots = _database.GetScreenshots(start, end, limit: 1000);

        foreach (var screenshot in screenshots)
        {
            var item = new TimelineItem
            {
                Id = screenshot.Id,
                Timestamp = screenshot.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                ProcessName = screenshot.ProcessName,
                WindowTitle = screenshot.WindowTitle,
                FilePath = screenshot.FilePath,
                Thumbnail = LoadThumbnail(screenshot.FilePath)
            };

            _items.Add(item);
        }
    }

    private Windows.UI.Xaml.Media.Imaging.BitmapImage? LoadThumbnail(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var bitmap = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
            using var stream = File.OpenRead(filePath);
            bitmap.SetSource(stream.AsRandomAccessStream());
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private void Screenshot_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TimelineItem item)
        {
            var viewer = new ScreenshotViewer(item);
            _ = viewer.ShowAsync();
        }
    }
}

public class TimelineItem
{
    public long Id { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public Windows.UI.Xaml.Media.Imaging.BitmapImage? Thumbnail { get; set; }
}
