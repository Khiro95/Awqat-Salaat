using System;
using System.Globalization;
using System.Windows.Data;

namespace AwqatSalaat.UI.Converters
{
    class ShortTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString(CultureInfo.InstalledUICulture.DateTimeFormat.ShortTimePattern, culture);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
