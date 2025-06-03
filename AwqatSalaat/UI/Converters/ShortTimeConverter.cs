using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace AwqatSalaat.UI.Converters
{
    class ShortTimeConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> s_cacheWithoutAMPM = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> s_cacheAMPMOnly = new Dictionary<string, string>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var shortPattern = CultureInfo.InstalledUICulture.DateTimeFormat.ShortTimePattern;
                var pattern = shortPattern;

                if (parameter is string format && !string.IsNullOrEmpty(format))
                {
                    if (format == "WithoutAMPM")
                    {
                        if (!s_cacheWithoutAMPM.TryGetValue(shortPattern, out pattern))
                        {
                            s_cacheWithoutAMPM[shortPattern] = pattern = shortPattern.Replace("t", "");
                        }
                    }
                    else if (format == "AMPMOnly")
                    {
                        if (!shortPattern.Contains('t'))
                        {
                            return string.Empty;
                        }

                        if (!s_cacheAMPMOnly.TryGetValue(shortPattern, out pattern))
                        {
                            s_cacheAMPMOnly[shortPattern] = pattern = new string(shortPattern.Where(c => c == 't').ToArray());
                        }
                    }
                }

                return dateTime.ToString(pattern, culture);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
