using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace PocxWallet.UI.Converters;

/// <summary>
/// Converts a boolean value to a Color (green for true, red for false)
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Color.Parse("#00C853") : Color.Parse("#FF3D00");
        }
        return Color.Parse("#6B7280"); // Gray for unknown
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a boolean value to one of two strings separated by |
/// Parameter format: "TrueValue|FalseValue"
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    public static readonly BoolToStringConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string paramStr)
        {
            var parts = paramStr.Split('|');
            if (parts.Length >= 2)
            {
                return boolValue ? parts[0] : parts[1];
            }
        }
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Returns true if the value is not null and not an empty collection
/// </summary>
public class IsNotNullOrEmptyConverter : IValueConverter
{
    public static readonly IsNotNullOrEmptyConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return false;
        if (value is string str) return !string.IsNullOrEmpty(str);
        if (value is int count) return count > 0;
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts boolean IsActive to a background color for parameter items
/// Active: subtle green tint, Inactive: standard background
/// </summary>
public class BoolToBackgroundConverter : IMultiValueConverter
{
    public static readonly BoolToBackgroundConverter Instance = new();
    
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 0 && values[0] is bool isActive)
        {
            return isActive 
                ? new SolidColorBrush(Color.Parse("#1A00C853")) // Subtle green tint
                : new SolidColorBrush(Color.Parse("#1A1E1E1E")); // Standard dark background
        }
        return new SolidColorBrush(Color.Parse("#1A1E1E1E"));
    }
}

/// <summary>
/// Converts boolean IsActive to a border color for parameter items
/// Active: green border, Inactive: standard border
/// </summary>
public class BoolToBorderConverter : IMultiValueConverter
{
    public static readonly BoolToBorderConverter Instance = new();
    
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 0 && values[0] is bool isActive)
        {
            return isActive 
                ? new SolidColorBrush(Color.Parse("#4000C853")) // Green border
                : new SolidColorBrush(Color.Parse("#333333")); // Standard border
        }
        return new SolidColorBrush(Color.Parse("#333333"));
    }
}

/// <summary>
/// Converts boolean IsActive to a foreground color for parameter name
/// Active: primary color, Inactive: secondary color
/// </summary>
public class BoolToForegroundConverter : IMultiValueConverter
{
    public static readonly BoolToForegroundConverter Instance = new();
    
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 0 && values[0] is bool isActive)
        {
            return isActive 
                ? new SolidColorBrush(Color.Parse("#E0E0E0")) // Bright text
                : new SolidColorBrush(Color.Parse("#9E9E9E")); // Dimmed text
        }
        return new SolidColorBrush(Color.Parse("#9E9E9E"));
    }
}
