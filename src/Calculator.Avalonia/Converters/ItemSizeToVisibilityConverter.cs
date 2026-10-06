// Port of CalculatorApp.Converters.ItemSizeToVisibilityConverter (+ negation).
// Avalonia uses bool IsVisible rather than a Visibility enum.

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CalculatorApp.Avalonia.Converters
{
    public class ItemSizeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int items && items == 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class ItemSizeToVisibilityNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int items && items > 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
