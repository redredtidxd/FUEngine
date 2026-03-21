using System;
using System.Globalization;
using System.Windows.Data;

namespace FUEngine;

public class BoolToPinLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "Desfijar" : "Fijar";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => System.Windows.Data.Binding.DoNothing;
}
