using Microsoft.UI.Xaml.Data;
using System;

namespace AwqatSalaat.WinUI.Converters
{
    internal class EnumEqualityConverter : IValueConverter
    {
        public static bool TestEquality(object value, object parameter)
        {
            if (value is Enum && parameter is string comparand && !string.IsNullOrEmpty(comparand))
            {
                return string.Equals(Enum.GetName(value.GetType(), value), comparand, StringComparison.Ordinal);
            }

            return false;
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return TestEquality(value, parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
