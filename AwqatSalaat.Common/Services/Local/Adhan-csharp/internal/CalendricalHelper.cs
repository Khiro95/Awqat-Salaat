using System;
using System.Globalization;

namespace Batoulapps.Adhan.Internal
{
    public class CalendricalHelper 
    {
        /// <summary>
        /// The Julian Day for a given Gregorian date
        /// </summary>
        /// <param name="year">the year</param>
        /// <param name="month">the month</param>
        /// <param name="day">the day</param>
        /// <returns>the julian day</returns>
        public static double JulianDay(int year, int month, int day) 
        {
            return JulianDay(year, month, day, 0.0);
        }

        /// <summary>
        /// The Julian Day for a given date
        /// </summary>
        /// <param name="date">the date</param>
        /// <returns>the julian day</returns>
        public static double JulianDay(DateTime date) 
        {
            DateTime calendar = date.ToUniversalTime();
            return JulianDay(calendar.Year, calendar.Month, 
                calendar.Day, calendar.Hour + calendar.Minute / 60.0);   
        }

        /// <summary>
        /// The Julian Day for a given Gregorian date
        /// </summary>
        /// <param name="year">the year</param>
        /// <param name="month">the month</param>
        /// <param name="day">the day</param>
        /// <param name="hours">hours</param>
        /// <returns>the julian day</returns>
        public static double JulianDay(int year, int month, int day, double hours) 
        {
            /* Equation from Astronomical Algorithms page 60 */

            // NOTE: Integer conversion is done intentionally for the purpose of decimal truncation

            int Y = month > 2 ? year : year - 1;
            int M = month > 2 ? month : month + 12;
            double D = day + (hours / 24);

            int A = Y/100;
            int B = 2 - A + (A/4);

            int i0 = (int) (365.25 * (Y + 4716));
            int i1 = (int) (30.6001 * (M + 1));
            return i0 + i1 + D + B - 1524.5;
        }

        /// <summary>
        /// Julian century from the epoch.
        /// </summary>
        /// <param name="JD">the julian day</param>
        /// <returns>the julian century from the epoch</returns>
        public static double JulianCentury(double JD) 
        {
            /* Equation from Astronomical Algorithms page 163 */
            return (JD - 2451545.0) / 36525;
        }
    }
}