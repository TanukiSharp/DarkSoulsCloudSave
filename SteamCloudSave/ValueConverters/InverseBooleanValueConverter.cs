using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SteamCloudSave.ValueConverters;

public class InverseBooleanValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Inverse(value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Inverse(value);
    }

    private static bool Inverse(object? value)
    {
        if (value is bool boolValue)
            return !boolValue;

        return value is null;
    }
}
