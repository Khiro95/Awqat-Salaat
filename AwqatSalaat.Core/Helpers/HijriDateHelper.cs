using System;
using System.Globalization;

namespace AwqatSalaat.Helpers
{
    public static class HijriDateHelper
    {
        private static readonly DateTimeFormatInfo FallbackHijriDateTimeFormatInfo;

        static HijriDateHelper()
        {
            // Start with the Arabic culture info since it provide all features
            var dateTimeFormat = new CultureInfo("ar-SA").DateTimeFormat;
            // Use English names as a fallback
            dateTimeFormat.MonthNames = new string[]
            {
                    "Muharram",
                    "Safar",
                    "Rabiʻ I",
                    "Rabiʻ II",
                    "Jumada I",
                    "Jumada II",
                    "Rajab",
                    "Shaʻban",
                    "Ramadan",
                    "Shawwal",
                    "Dhuʻl-Qiʻdah",
                    "Dhuʻl-Hijjah",
                    ""
            };
            // MonthGenitiveNames is what provide values for formatter
            dateTimeFormat.MonthGenitiveNames = dateTimeFormat.MonthNames;

            FallbackHijriDateTimeFormatInfo = dateTimeFormat;
        }

        public static string Format(DateTime dateTime, string format, string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            }

            if (language.Length > 2)
            {
                language = language.Substring(0, 2);
            }

            language += "-SA";

            CultureInfo culture = CultureInfo.GetCultureInfo(language);

            DateTimeFormatInfo dateTimeFormat = culture.DateTimeFormat;

            if (dateTimeFormat.Calendar.GetType() != typeof(UmAlQuraCalendar))
            {
                if (culture.TextInfo.IsRightToLeft)
                {
                    dateTimeFormat = CultureInfo.GetCultureInfo("ar-SA").DateTimeFormat;
                }
                else
                {
                    dateTimeFormat = FallbackHijriDateTimeFormatInfo;
                }
            }

            return dateTime.ToString(format, dateTimeFormat);
        }
    }
}
