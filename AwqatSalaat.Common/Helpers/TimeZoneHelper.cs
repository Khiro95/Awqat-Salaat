using System;
using System.Collections.Generic;
using TimeZoneConverter;

namespace AwqatSalaat.Helpers
{
    internal static class TimeZoneHelper
    {
        private static readonly Dictionary<string, TimeZoneInfo> tzInfoCache = new Dictionary<string, TimeZoneInfo>();

        public static string GetIanaTimeZone()
        {
            var id = TimeZoneInfo.Local.Id;

            if (TZConvert.TryWindowsToIana(id, out string ianaTimeZone))
            {
                return ianaTimeZone;
            }

            return null;
        }

        public static DateTime ConvertDateTimeToLocal(DateTime dateTime, string ianaTimeZone)
            => ConvertDateTimeToLocal(dateTime, ianaTimeZone, TimeZoneInfo.Local);

        internal static DateTime ConvertDateTimeToLocal(DateTime dateTime, string ianaTimeZone, TimeZoneInfo localTimeZone)
        {
            TimeZoneInfo timeZoneInfo = null;

            if (!string.IsNullOrEmpty(ianaTimeZone) && !tzInfoCache.TryGetValue(ianaTimeZone, out timeZoneInfo))
            {
                if (TZConvert.TryIanaToWindows(ianaTimeZone, out string timeZoneId))
                {
                    timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

                    tzInfoCache[ianaTimeZone] = timeZoneInfo;
                }
            }

            // Zones with the same base offset can still differ in daylight saving (e.g. Berlin and Lagos are both
            // UTC+1, but Berlin is UTC+2 in summer), so only skip the conversion when the rules are the same
            if (timeZoneInfo is null || timeZoneInfo.HasSameRules(localTimeZone))
            {
                return dateTime;
            }

            dateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, timeZoneInfo);

            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, localTimeZone);
        }

        public static DateTime ConvertDateTimeOffsetToLocal(DateTimeOffset dateTimeOffset)
            => ConvertDateTimeOffsetToLocal(dateTimeOffset, TimeZoneInfo.Local);

        internal static DateTime ConvertDateTimeOffsetToLocal(DateTimeOffset dateTimeOffset, TimeZoneInfo localTimeZone)
        {
            var local = TimeZoneInfo.ConvertTime(dateTimeOffset, localTimeZone);

            return DateTime.SpecifyKind(local.DateTime, DateTimeKind.Unspecified);
        }
    }
}
