using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace FUEngine;

public class PathToFileNameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path))
            return Path.GetFileName(path);
        return value;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => System.Windows.Data.Binding.DoNothing;
}
