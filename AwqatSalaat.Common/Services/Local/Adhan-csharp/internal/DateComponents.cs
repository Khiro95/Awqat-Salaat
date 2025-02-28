using System;

namespace Batoulapps.Adhan.Internal
{
    public class DateComponents 
    {
        public readonly int Year;
        public readonly int Month;
        public readonly int Day;
    
        /// <summary>
        /// Convenience method that returns a DateComponents from a given Date
        /// </summary>
        /// <param name="date">the date</param>
        /// <returns>the DateComponents (according to the default device timezone)</returns>
        public static DateComponents From(DateTime date) 
        {            
            return new DateComponents(date.Year, date.Month, date.Day);
        }

        public DateComponents(int year, int month, int day) 
        {
            this.Year = year;
            this.Month = month;
            this.Day = day;
        }
    }
}