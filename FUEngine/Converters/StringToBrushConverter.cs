using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FUEngine;

public class StringToBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(s);
                return new SolidColorBrush(color);
            }
            catch { /* ignore */ }
        }
        return null;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => System.Windows.Data.Binding.DoNothing;
}
