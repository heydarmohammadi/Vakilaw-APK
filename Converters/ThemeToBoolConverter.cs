using System.Globalization;

namespace Vakilaw.Converters
{
    public class ThemeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return parameter.ToString();
            return null;
        }
    }
}