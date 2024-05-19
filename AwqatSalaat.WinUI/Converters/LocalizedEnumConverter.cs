using AwqatSalaat.Helpers;
using Microsoft.UI.Xaml.Data;
using System;

namespace AwqatSalaat.WinUI.Converters
{
    internal class LocalizedEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Enum && parameter is string format && !string.IsNullOrEmpty(format))
            {
                return LocaleManager.Default.Get(string.Format(format, value));
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
