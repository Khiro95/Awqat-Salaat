using System;

namespace Batoulapps.Adhan.Internal
{
    public static class DateTimeExtensions
    {  
        public static TimeSpan Round(this TimeSpan time, TimeSpan roundingInterval, MidpointRounding roundingType) 
        {
            return new TimeSpan(
                Convert.ToInt64(Math.Round(
                    time.Ticks / (decimal)roundingInterval.Ticks,
                    roundingType
                )) * roundingInterval.Ticks
            );
        }

        public static TimeSpan Round(this TimeSpan time, TimeSpan roundingInterval) {
            return Round(time, roundingInterval, MidpointRounding.ToEven);
        }

        public static DateTime Round(this DateTime datetime, TimeSpan roundingInterval) {
            return new DateTime((datetime - DateTime.MinValue).Round(roundingInterval).Ticks);
        }      

        /// <summary>
        /// Returns the number of milliseconds since 01/01/1970
        /// </summary>
        /// <param name="datetime">the date time</param>
        /// <returns>the number of milliseconds since 01/01/1970</returns>
        public static double GetTime(this DateTime datetime)
        {
            //DateTime.MinValue is 01/01/01 00:00 so add 1969 years. to get 1/1/1970
            return datetime.Subtract(DateTime.MinValue.AddYears(1969)).TotalMilliseconds;
        }  

        public static bool Before(this DateTime datetime, DateTime compareDateTime)
        {
            return datetime < compareDateTime;
        }

        public static bool After(this DateTime datetime, DateTime compareDateTime)
        {
            return datetime > compareDateTime;
        }
    }
}