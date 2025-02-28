namespace Batoulapps.Adhan
{
    /// <summary>
    /// Rules for dealing with Fajr and Isha at places with high latitudes
    /// </summary>
    public enum HighLatitudeRule 
    {
        /// <summary>
        /// Fajr will never be earlier than the middle of the night, and Isha will never be later than
        /// the middle of the night.
        /// </summary>
        /// <value></value>
        MIDDLE_OF_THE_NIGHT,
  
        /// <summary>
        /// Fajr will never be earlier than the beginning of the last seventh of the night, and Isha will
        /// never be later than the end of hte first seventh of the night.
        /// </summary>
        /// <value></value>
        SEVENTH_OF_THE_NIGHT,
          
        /// <summary>
        /// Similar to {@link HighLatitudeRule#SEVENTH_OF_THE_NIGHT}, but instead of 1/7th, the faction
        /// of the night used is fajrAngle / 60 and ishaAngle/60.
        /// </summary>
        /// <value></value>
        TWILIGHT_ANGLE
    }
}
