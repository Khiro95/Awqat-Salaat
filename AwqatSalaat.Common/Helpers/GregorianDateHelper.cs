using System;
using System.Globalization;
using System.Linq;

namespace AwqatSalaat.Helpers
{
    public static class GregorianDateHelper
    {
        public static MonthRecord[] GregorianMonths { get; }

        static GregorianDateHelper()
        {
            GregorianMonths = Enumerable.Range(1, 12).Select(n => new MonthRecord(n, new DateTime(2024, n, 1))).ToArray();
        }

        public static string Format(DateTime dateTime, string format, string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            }

            CultureInfo culture = CultureInfo.CreateSpecificCulture(language);

            if (language == "ar")
            {
                var calendar = culture.OptionalCalendars.Single(c => c is GregorianCalendar gc && gc.CalendarType == GregorianCalendarTypes.Localized);
                culture.DateTimeFormat.Calendar = calendar;
            }

            return dateTime.ToString(format, culture.DateTimeFormat);
        }
    }
}
