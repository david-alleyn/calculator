// Port of CalculatorApp.Converters.BooleanNegationConverter.

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CalculatorApp.Avalonia.Converters
{
    public class BooleanNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }
    }
}
