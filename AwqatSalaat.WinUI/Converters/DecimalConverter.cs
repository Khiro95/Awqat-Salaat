using Microsoft.UI.Xaml.Data;
using System;

namespace AwqatSalaat.WinUI.Converters
{
    //https://github.com/microsoft/microsoft-ui-xaml/issues/9064
    internal class DecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is decimal number)
            {
                return number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string numberString && targetType == typeof(decimal))
            {
                try
                {
                    return System.Convert.ToDecimal(numberString, System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (FormatException fex)
                {
                    return decimal.Zero;
                }
            }

            return null;
        }
    }
}
