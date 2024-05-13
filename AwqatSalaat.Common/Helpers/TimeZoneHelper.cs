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

            if (timeZoneInfo is null || timeZoneInfo.BaseUtcOffset == TimeZoneInfo.Local.BaseUtcOffset)
            {
                return dateTime;
            }

            dateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, timeZoneInfo);

            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, TimeZoneInfo.Local);
        }
    }
}
