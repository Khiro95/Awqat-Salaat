using AwqatSalaat.Helpers;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AwqatSalaat.UI.Converters
{
    class LocalizedEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum && parameter is string format && !string.IsNullOrEmpty(format))
            {
                return LocaleManager.Default.Get(string.Format(format, value));
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
