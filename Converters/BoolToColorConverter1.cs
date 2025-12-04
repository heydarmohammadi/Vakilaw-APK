using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Vakilaw.Converters;

public class BoolToColorConverter1 : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isIncome)
        {
            return isIncome ? Colors.GreenYellow : Colors.Crimson;
        }
        return Colors.DodgerBlue; // پیش‌فرض
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}