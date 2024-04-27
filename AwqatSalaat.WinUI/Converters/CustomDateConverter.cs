using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace AwqatSalaat.WinUI.Converters
{
    internal class CustomDateTimeConverter : IValueConverter
    {
        public static string Format(DateTime dateTime, string format, string language)
        {
            CultureInfo culture = string.IsNullOrEmpty(language) ? CultureInfo.CurrentUICulture : CultureInfo.GetCultureInfo(language);
            return dateTime.ToString(format, culture);
        }

        public static string FormatShortTime(DateTime dateTime, CultureInfo culture)
        {
            culture ??= CultureInfo.CurrentUICulture;
            return dateTime.ToString(CultureInfo.InstalledUICulture.DateTimeFormat.ShortTimePattern, culture);
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dateTime)
            {
                return Format(dateTime, parameter as string, language);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
