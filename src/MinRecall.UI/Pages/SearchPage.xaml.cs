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
using System.Threading.Tasks;

namespace MinRecall.UI.Pages;

public sealed partial class SearchPage : Page
{
    private DatabaseManager? _database;
    private ObservableCollection<SearchResultItem> _results = new();
    private DispatcherTimer? _searchTimer;

    public SearchPage()
    {
        this.InitializeComponent();
        SearchResultsControl.ItemsSource = _results;

        _searchTimer = new DispatcherTimer();
        _searchTimer.Interval = TimeSpan.FromMilliseconds(500);
        _searchTimer.Tick += SearchTimer_Tick;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MinRecall"
        );

        _database = new DatabaseManager(Path.Combine(appDataPath, "minrecall.db"));
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            _searchTimer?.Stop();
            _searchTimer?.Start();
        }
    }

    private async void SearchTimer_Tick(object? sender, object e)
    {
        _searchTimer?.Stop();
        await PerformSearch();
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        _searchTimer?.Stop();
        _ = PerformSearch();
    }

    private async Task PerformSearch()
    {
        if (_database == null) return;

        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
        {
            _results.Clear();

            var searchTerm = SearchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return;
            }

            DateTime? start = null;
            DateTime? end = null;

            if (StartDatePicker.Date.HasValue)
            {
                start = StartDatePicker.Date.Value.DateTime;
            }

            if (EndDatePicker.Date.HasValue)
            {
                end = EndDatePicker.Date.Value.DateTime.AddDays(1);
            }

            var screenshots = _database.SearchScreenshots(searchTerm, start, end, limit: 100);

            foreach (var screenshot in screenshots)
            {
                var item = new SearchResultItem
                {
                    Id = screenshot.Id,
                    Timestamp = screenshot.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    ProcessName = screenshot.ProcessName,
                    WindowTitle = screenshot.WindowTitle,
                    FilePath = screenshot.FilePath,
                    Thumbnail = LoadThumbnail(screenshot.FilePath),
                    SearchTerm = searchTerm
                };

                _results.Add(item);
            }
        });
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

    private void SearchResult_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is SearchResultItem item)
        {
            var viewer = new ScreenshotViewer(new TimelineItem
            {
                Id = item.Id,
                Timestamp = item.Timestamp,
                ProcessName = item.ProcessName,
                WindowTitle = item.WindowTitle,
                FilePath = item.FilePath,
                Thumbnail = item.Thumbnail
            });
            _ = viewer.ShowAsync();
        }
    }
}

public class SearchResultItem
{
    public long Id { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public Windows.UI.Xaml.Media.Imaging.BitmapImage? Thumbnail { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public string MatchedText => GetMatchedText();

    private string GetMatchedText()
    {
        var matched = WindowTitle;
        if (matched.Length > 100)
        {
            matched = matched.Substring(0, 100) + "...";
        }
        return $"Match: {matched}";
    }
}
