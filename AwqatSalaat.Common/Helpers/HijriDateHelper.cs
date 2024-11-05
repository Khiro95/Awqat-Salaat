using AwqatSalaat.Properties;
using System;
using System.Globalization;
using System.Linq;

namespace AwqatSalaat.Helpers
{
    public static class HijriDateHelper
    {
        private static readonly DateTimeFormatInfo s_ArabicUmAlQuraDateTimeFormatInfo;
        private static readonly DateTimeFormatInfo s_EnglishUmAlQuraDateTimeFormatInfo;
        private static readonly DateTimeFormatInfo s_ArabicHijriDateTimeFormatInfo;
        private static readonly DateTimeFormatInfo s_EnglishHijriDateTimeFormatInfo;
        private static readonly HijriCalendar s_HijriCalendar = new HijriCalendar();

        public static MonthRecord[] HijriMonths { get; }

        static HijriDateHelper()
        {
            var calendar = new UmAlQuraCalendar();
            HijriMonths = new MonthRecord[12];

            for (int i = 1; i <= 12; i++)
            {
                // Here 15 is a safe choice to make sure both HijriCaledar and UmAlQuraCalendar
                // return same month when we convert later
                HijriMonths[i - 1] = new MonthRecord(i, calendar.ToDateTime(1446, i, 15, 0, 0, 0, 0));
            }

            var englishHijriMonths = new string[]
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

            // Init Arabc DateTimeFormatInfo for Um Al Qura calendar
            var arSA = new CultureInfo("ar-SA");
            s_ArabicUmAlQuraDateTimeFormatInfo = arSA.DateTimeFormat;

            if (!(arSA.Calendar is UmAlQuraCalendar))
            {
                var umalqura = arSA.OptionalCalendars.Single(c => c is UmAlQuraCalendar);
                s_ArabicUmAlQuraDateTimeFormatInfo.Calendar = umalqura;
            }



            // Init English DateTimeFormatInfo for Um Al Qura calendar
            var enSA = new CultureInfo("en-SA");

            if (enSA.OptionalCalendars.Any(c =>  c is UmAlQuraCalendar))
            {
                s_EnglishUmAlQuraDateTimeFormatInfo = enSA.DateTimeFormat;

                if (!(enSA.Calendar is UmAlQuraCalendar))
                {
                    var umalqura = enSA.OptionalCalendars.Single(c => c is UmAlQuraCalendar);
                    s_EnglishUmAlQuraDateTimeFormatInfo.Calendar = umalqura;
                }
            }
            else
            {
                // Start with the Arabic culture info since it provide all features
                var dateTimeFormat = new CultureInfo("ar-SA").DateTimeFormat;
                // Set English names
                dateTimeFormat.MonthNames = englishHijriMonths;
                // MonthGenitiveNames is what provide values for formatter
                dateTimeFormat.MonthGenitiveNames = dateTimeFormat.MonthNames;

                s_EnglishUmAlQuraDateTimeFormatInfo = dateTimeFormat;
            }



            // Init Arabic DateTimeFormatInfo for Hijri calendar
            arSA = new CultureInfo("ar-SA");
            arSA.DateTimeFormat.Calendar = s_HijriCalendar;
            s_ArabicHijriDateTimeFormatInfo = arSA.DateTimeFormat;



            // Init English DateTimeFormatInfo for Hijri calendar
            enSA = new CultureInfo("en-SA");

            if (enSA.OptionalCalendars.Any(c => c is HijriCalendar))
            {
                enSA.DateTimeFormat.Calendar = s_HijriCalendar;
                s_EnglishHijriDateTimeFormatInfo = enSA.DateTimeFormat;
            }
            else
            {
                // Start with the Arabic culture info since it provide all features
                var dateTimeFormat = new CultureInfo("ar-SA").DateTimeFormat;
                dateTimeFormat.Calendar = s_HijriCalendar;
                // Set English names
                dateTimeFormat.MonthNames = englishHijriMonths;
                // MonthGenitiveNames is what provide values for formatter
                dateTimeFormat.MonthGenitiveNames = dateTimeFormat.MonthNames;

                s_EnglishHijriDateTimeFormatInfo = dateTimeFormat;
            }



            Settings.Default.SettingsLoaded += (_, __) => s_HijriCalendar.HijriAdjustment = Settings.Default.HijriAdjustment;
            Settings.Default.SettingsSaving += (_, __) => s_HijriCalendar.HijriAdjustment = Settings.Default.HijriAdjustment;
            s_HijriCalendar.HijriAdjustment = Settings.Default.HijriAdjustment;
        }

        public static string Format(DateTime dateTime, string format, string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            }

            CultureInfo culture = CultureInfo.GetCultureInfo(language);

            DateTimeFormatInfo dateTimeFormat = GetProperDateTimeFormatInfo(culture);

            return dateTime.ToString(format, dateTimeFormat);
        }

        private static DateTimeFormatInfo GetProperDateTimeFormatInfo(CultureInfo culture)
        {
            var calendarType = Settings.Default.CalendarType;

            if (calendarType == Data.CalendarType.UmAlQura)
            {
                return culture.TextInfo.IsRightToLeft ? s_ArabicUmAlQuraDateTimeFormatInfo : s_EnglishUmAlQuraDateTimeFormatInfo;
            }
            else if (calendarType == Data.CalendarType.Hijri)
            {
                return culture.TextInfo.IsRightToLeft ? s_ArabicHijriDateTimeFormatInfo : s_EnglishHijriDateTimeFormatInfo;
            }

            throw new InvalidOperationException("Invalid calendar type.");
        }
    }
}
