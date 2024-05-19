using Microsoft.UI.Xaml.Data;
using System;

namespace AwqatSalaat.WinUI.Converters
{
    internal class BooleanInvertedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
            {
                return !b;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
