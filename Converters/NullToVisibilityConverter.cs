using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DynamicIsland.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        if (value is string s)
        {
            isNull = string.IsNullOrWhiteSpace(s);
        }

        if (Invert)
        {
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        return isNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
