using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.IO;

namespace MinRecall.UI.Converters;

public class FilePathToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string filePath && !string.IsNullOrEmpty(filePath))
        {
            try
            {
                // Check if file exists
                if (File.Exists(filePath))
                {
                    // Load the image from file
                    return new Bitmap(filePath);
                }
                
                Console.WriteLine($"[WARN] Image file not found: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load image from {filePath}: {ex.Message}");
            }
        }
        
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
