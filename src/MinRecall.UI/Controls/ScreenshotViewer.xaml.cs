using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MinRecall.UI.Controls;

public sealed partial class ScreenshotViewer : ContentDialog
{
    private string _filePath = string.Empty;
    private BitmapImage? _bitmap;

    public ScreenshotViewer(TimelineItem item)
    {
        this.InitializeComponent();
        this.Loaded += ScreenshotViewer_Loaded;

        _filePath = item.FilePath;
        ProcessNameText.Text = item.ProcessName;
        WindowTitleText.Text = item.WindowTitle;
        TimestampText.Text = item.Timestamp;

        if (item.Thumbnail != null)
        {
            ScreenshotImage.Source = item.Thumbnail;
        }
    }

    private async void ScreenshotViewer_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadFullImage();
    }

    private async System.Threading.Tasks.Task LoadFullImage()
    {
        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
        {
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            using (var stream = File.OpenRead(_filePath))
            {
                await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
            }

            _bitmap = bitmap;
            ScreenshotImage.Source = bitmap;
        }
        catch
        {
            // Use thumbnail if full load fails
        }
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        // Button has a flyout menu
    }

    private async void ExportPng_Click(object sender, RoutedEventArgs e)
    {
        await ExportImage(".png");
    }

    private async void ExportJpeg_Click(object sender, RoutedEventArgs e)
    {
        await ExportImage(".jpg");
    }

    private async System.Threading.Tasks.Task ExportImage(string extension)
    {
        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
        {
            return;
        }

        var window = new Window();
        var hwnd = WindowNative.GetWindowHandle(window);
        var picker = new FileSavePicker();

        InitializeWithWindow.Initialize(picker, hwnd);

        picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        picker.FileTypeChoices.Add("Image", new[] { extension });
        picker.SuggestedFileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";

        var file = await picker.PickSaveFileAsync();
        if (file != null)
        {
            try
            {
                await File.CopyAsync(_filePath, file.Path);
            }
            catch
            {
                // Handle error
            }
        }

        this.Hide();
    }

    private async void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
        {
            return;
        }

        try
        {
            var file = await StorageFile.GetFileFromPathAsync(_filePath);
            var dataPackage = new DataPackage();
            dataPackage.SetStorageItems(new[] { file });
            Clipboard.SetContent(dataPackage);
        }
        catch
        {
            // Handle error
        }
    }

    private async System.Threading.Tasks.Task FileCopyAsync(string source, string destination)
    {
        using var sourceStream = File.OpenRead(source);
        using var destinationStream = File.Create(destination);
        await sourceStream.CopyToAsync(destinationStream);
    }
}
