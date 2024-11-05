using AwqatSalaat.Helpers;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AwqatSalaat.UI.Converters
{
    internal class GregorianDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return GregorianDateHelper.Format(dateTime, parameter as string, LocaleManager.Default.Current);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
