using System;
using System.Globalization;
using System.Windows;

namespace Acklann.Scafman.Converters
{
    public class VisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool.TryParse((parameter?.ToString() ?? bool.FalseString), out bool negate);

            if (value is bool v)
                if (negate)
                    return (v ? Visibility.Collapsed : Visibility.Visible);
                else
                    return (v ? Visibility.Visible : Visibility.Collapsed);

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
        }
    }
}