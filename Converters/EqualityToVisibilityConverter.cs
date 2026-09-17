using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace workspace_hub.Converters
{
    // Compares two strings: [0] = current folder, [1] = primary folder
    // Returns Visible if equal, otherwise Collapsed. If parameter is "Invert" it inverts the result.
    public class EqualityToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var invert = parameter as string == "Invert";
            if (values == null || values.Length < 2) return Visibility.Collapsed;

            var a = values[0] as string;
            var b = values[1] as string;

            var equal = string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
            if (invert) equal = !equal;

            return equal ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
