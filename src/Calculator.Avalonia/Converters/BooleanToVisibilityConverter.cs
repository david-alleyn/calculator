// Port of CalculatorApp.Converters.BooleanToVisibilityConverter.
// Avalonia uses bool IsVisible rather than a Visibility enum, so this
// converter is an identity for bools (kept for XAML readability parity).

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CalculatorApp.Avalonia.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b;
        }
    }
}
