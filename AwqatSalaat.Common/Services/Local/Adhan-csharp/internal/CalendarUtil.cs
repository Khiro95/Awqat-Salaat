using System;

namespace Batoulapps.Adhan.Internal
{
    public class CalendarUtil 
    {
        public static bool IsLeapYear(int year)
        {
            return year % 4 == 0 && !(year % 100 == 0 && year % 400 != 0);
        }

        /// <summary>
        /// Date and time with a rounded minute
        /// This returns a date with the seconds rounded and added to the minute
        /// </summary>
        /// <param name="when">the date and time</param>
        /// <returns>the date and time with 0 seconds and minutes including rounded seconds</returns>
        public static DateTime RoundedMinute(DateTime when)
        {
            return new DateTime((when - DateTime.MinValue).Round(TimeSpan.FromMinutes(1)).Ticks);
        }

        /// <summary>
        /// Gets a date for the particular date
        /// </summary>
        /// <param name="components">the date components</param>
        /// <returns>the date with a time set to 00:00:00 at utc</returns>
        public static DateTime ResolveTime(DateComponents components) {
            return new DateTime(components.Year, components.Month, components.Day, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}