using System;

namespace Batoulapps.Adhan.Internal
{
    public class TimeComponents 
    {
        public readonly int Hours;
        public readonly int Minutes;
        public readonly int Seconds;

        public static TimeComponents FromDouble(double value) 
        {
            if (value == Double.MaxValue || Double.IsNaN(value)) 
            {
                return null;
            }

            double hours = Math.Floor(value);
            double minutes = Math.Floor((value - hours) * 60.0);
            double seconds = Math.Floor((value - (hours + minutes / 60.0)) * 60 * 60);
            return new TimeComponents((int) hours, (int) minutes, (int) seconds);
        }

        private TimeComponents(int hours, int minutes, int seconds) 
        {
            this.Hours = hours;
            this.Minutes = minutes;
            this.Seconds = seconds;
        }

        public DateTime? DateComponents(DateTime date) 
        {
            return date.Date.AddHours(Hours).AddMinutes(Minutes).AddSeconds(Seconds);            
        }
    }
}