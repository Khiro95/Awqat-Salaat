namespace Batoulapps.Adhan
{
    /// <summary>
    /// Adjustment value for prayer times, in minutes
    /// These values are added (or subtracted) from the prayer time that is calculated before
    /// returning the result times.
    /// </summary>
    public class PrayerAdjustments
    {
        /// <summary>
        /// Fajr offset in minutes
        /// </summary>
        public int Fajr = 0;

        /// <summary>
        /// Sunrise offset in minutes
        /// </summary>
        public int Sunrise = 0;

        /// <summary>
        /// Dhuhr offset in minutes
        /// </summary>
        public int Dhuhr = 0;

        /// <summary>
        /// Asr offset in minutes
        /// </summary>
        public int Asr = 0;

        /// <summary>
        /// Maghrib offset in minutes
        /// </summary>
        public int Maghrib = 0;

        /// <summary>
        /// Isha offset in minutes
        /// </summary>
        public int Isha = 0;

        /// <summary>
        /// Gets a PrayerAdjustments object with all offsets set to 0
        /// </summary>
        public PrayerAdjustments()
        {
        }

        /// <summary>
        /// Gets a PrayerAdjustments object to offset prayer times
        /// </summary>
        /// <param name="fajr">offset from fajr in minutes</param>
        /// <param name="sunrise">offset from sunrise in minutes</param>
        /// <param name="dhuhr">offset from dhuhr in minutes</param>
        /// <param name="asr">offset from asr in minutes</param>
        /// <param name="maghrib">offset from maghrib in minutes</param>
        /// <param name="isha">offset from isha in minutes</param>
        public PrayerAdjustments(int fajr, int sunrise, int dhuhr, int asr, int maghrib, int isha)
        {
            this.Fajr = fajr;
            this.Sunrise = sunrise;
            this.Dhuhr = dhuhr;
            this.Asr = asr;
            this.Maghrib = maghrib;
            this.Isha = isha;
        }
    }
}