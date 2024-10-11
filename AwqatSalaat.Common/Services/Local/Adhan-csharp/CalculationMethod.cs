using System;

namespace Batoulapps.Adhan
{
    /// <summary>
    /// Standard calculation methods for calculating prayer times
    /// </summary>
    public enum CalculationMethod 
    {
        /// <summary>
        /// Muslim World League
        /// Uses Fajr angle of 18 and an Isha angle of 17
        /// </summary>
        /// <value></value>
        MUSLIM_WORLD_LEAGUE,
            
        /// <summary>
        /// Egyptian General Authority of Survey
        /// Uses Fajr angle of 19.5 and an Isha angle of 17.5
        /// </summary>
        /// <value></value>
        EGYPTIAN,

        /// <summary>
        /// University of Islamic Sciences, Karachi
        /// Uses Fajr angle of 18 and an Isha angle of 18
        /// </summary>
        /// <value></value>
        KARACHI,

        /// <summary>
        /// Umm al-Qura University, Makkah
        /// Uses a Fajr angle of 18.5 and an Isha angle of 90. Note: You should add a +30 minute custom
        ///  adjustment of Isha during Ramadan.
        /// </summary>
        /// <value></value>
        UMM_AL_QURA,

        /// <summary>
        /// The Gulf Region
        /// Uses Fajr and Isha angles of 18.2 degrees.
        /// </summary>
        /// <value></value>
        DUBAI,

        /// <summary>
        /// Moonsighting Committee
        /// Uses a Fajr angle of 18 and an Isha angle of 18. Also uses seasonal adjustment values.
        /// </summary>
        /// <value></value>
        MOON_SIGHTING_COMMITTEE,

        /// <summary>
        /// Referred to as the ISNA method
        /// This method is included for completeness, but is not recommended.
        /// Uses a Fajr angle of 15 and an Isha angle of 15.
        /// </summary>
        /// <value></value>
        NORTH_AMERICA,

        /// <summary>
        /// Kuwait
        /// Uses a Fajr angle of 18 and an Isha angle of 17.5
        /// </summary>
        /// <value></value>
        KUWAIT,

        /// <summary>
        /// Qatar
        /// Modified version of Umm al-Qura that uses a Fajr angle of 18.
        /// </summary>
        /// <value></value>
        QATAR,

        /// <summary>
        /// Singapore
        /// Uses a Fajr angle of 20 and an Isha angle of 18
        /// </summary>
        /// <value></value>
        SINGAPORE,

        /// <summary>
        /// The default value for {@link CalculationParameters#method} when initializing a
        /// {@link CalculationParameters} object. Sets a Fajr angle of 0 and an Isha angle of 0.
        /// </summary>
        /// <returns></returns>
        OTHER
    }

    public static class CalculationMethodExtensions
    {
      public static CalculationParameters GetParameters(this CalculationMethod method)
      {
        switch(method)
        {
          case CalculationMethod.MUSLIM_WORLD_LEAGUE:
            return new CalculationParameters(18.0, 17.0, method)
              .withMethodAdjustments(new PrayerAdjustments(0, 0, 1, 0, 0, 0));
          case CalculationMethod.EGYPTIAN: 
            return new CalculationParameters(20.0, 18.0, method)
              .withMethodAdjustments(new PrayerAdjustments(0, 0, 1, 0, 0, 0));      
          case CalculationMethod.KARACHI: 
            return new CalculationParameters(18.0, 18.0, method)
                .withMethodAdjustments(new PrayerAdjustments(0, 0, 1, 0, 0, 0));
          case CalculationMethod.UMM_AL_QURA: 
            return new CalculationParameters(18.5, 90, method);
          case CalculationMethod.DUBAI: 
            return new CalculationParameters(18.2, 18.2, method)
                .withMethodAdjustments(new PrayerAdjustments(0, -3, 3, 3, 3, 0));
          case CalculationMethod.MOON_SIGHTING_COMMITTEE: 
            return new CalculationParameters(18.0, 18.0, method)
                .withMethodAdjustments(new PrayerAdjustments(0, 0, 5, 0, 3, 0));
          case CalculationMethod.NORTH_AMERICA: 
            return new CalculationParameters(15.0, 15.0, method)
                .withMethodAdjustments(new PrayerAdjustments(0, 0, 1, 0, 0, 0));
          case CalculationMethod.KUWAIT: 
            return new CalculationParameters(18.0, 17.5, method);
          case CalculationMethod.QATAR: 
            return new CalculationParameters(18.0, 90, method);
          case CalculationMethod.SINGAPORE: 
            return new CalculationParameters(20.0, 18.0, method)
                .withMethodAdjustments(new PrayerAdjustments(0, 0, 1, 0, 0, 0));
          case CalculationMethod.OTHER: 
            return new CalculationParameters(0.0, 0.0, method);
          default: 
            throw new ArgumentException("Invalid CalculationMethod");
        }
      }
    }
}