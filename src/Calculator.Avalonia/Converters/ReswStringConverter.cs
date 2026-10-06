// Converts a bound value (ignored) into a localized resource string whose key
// is passed via ConverterParameter. Lets templates carry localized labels
// without code-behind.

using System;
using System.Globalization;
using Avalonia.Data.Converters;

using CalculatorApp.ViewModel.Common;

namespace CalculatorApp.Avalonia.Converters
{
    public class ReswStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key = parameter as string;
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            return AppResourceProvider.GetInstance().GetResourceString(key);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
