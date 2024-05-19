using System;
using System.Globalization;
using System.Windows.Data;

namespace AwqatSalaat.UI.Converters
{
    internal class TimeSpanFormatConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length == 2 && values[0] is TimeSpan time && values[1] is string format)
            {
                if (format.Contains("{") && format.Contains("}"))
                {
                    return string.Format(format, time);
                }
                else
                {
                    return time.ToString(format); 
                }
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
