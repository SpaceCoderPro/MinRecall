using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace MinRecall.UI.Converters;

public class ActivityToColorConverter : IValueConverter
{
    public static readonly ActivityToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int activity)
        {
            return activity switch
            {
                >= 15 => new SolidColorBrush(0xFF00ff88), // Green for high activity
                >= 10 => new SolidColorBrush(0xFF00d9ff), // Cyan for medium activity
                >= 5 => new SolidColorBrush(0xFFffaa00), // Orange for low activity
                _ => new SolidColorBrush(0xFF666666) // Gray for no activity
            };
        }
        return new SolidColorBrush(0xFF666666);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class ActivityToWidthConverter : IValueConverter
{
    public static readonly ActivityToWidthConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int activity)
        {
            return Math.Max(4, activity * 8); // Minimum 4px, max based on activity
        }
        return 4;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class DurationConverter : IValueConverter
{
    public static readonly DurationConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime endTime)
        {
            // Get the start time from the data context if available
            // For now, return a placeholder since we're dealing with individual properties
            return "45m"; // Placeholder - in a real implementation this would be calculated properly
        }
        
        return "0m";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class InverseBooleanConverter : IValueConverter
{
    public static readonly InverseBooleanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}