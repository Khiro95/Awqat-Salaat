using System;

namespace Batoulapps.Adhan
{
    /// <summary>
    /// Parameters used for PrayerTime calculation customization
    /// 
    /// Note that, for many cases, you can use {@link CalculationMethod#getParameters()} to get a
    /// pre-computed set of calculation parameters depending on one of the available
    /// {@link CalculationMethod}.
    /// </summary>
    public class CalculationParameters 
    {
        /// <summary>
        /// The method used to do the calculation
        /// </summary>
        public CalculationMethod Method = CalculationMethod.OTHER;        

        /// <summary>
        /// The angle of the sun used to calculate fajr
        /// </summary>
        public double FajrAngle;

        /// <summary>
        /// The angle of the sun used to calculate isha
        /// </summary>
        public double IshaAngle;

        /// <summary>
        /// Minutes after Maghrib (if set, the time for Isha will be Maghrib plus IshaInterval)
        /// </summary>
        public int IshaInterval = 0;
        
        /// <summary>
        /// The madhab used to calculate Asr
        /// </summary>
        public Madhab Madhab = Madhab.SHAFI;
  
        /// <summary>
        /// Rules for placing bounds on Fajr and Isha for high latitude areas
        /// </summary>
        public HighLatitudeRule HighLatitudeRule = HighLatitudeRule.MIDDLE_OF_THE_NIGHT;
  
        /// <summary>
        /// Used to optionally add or subtract a set amount of time from each prayer time
        /// </summary>
        /// <returns></returns>
        public PrayerAdjustments Adjustments = new PrayerAdjustments();
        
        /// <summary>
        /// Used for method adjustments
        /// </summary>
        /// <returns></returns>
        public PrayerAdjustments MethodAdjustments = new PrayerAdjustments();

        /// <summary>
        /// Generate CalculationParameters from angles
        /// </summary>
        /// <param name="fajrAngle">the angle for calculating fajr</param>
        /// <param name="ishaAngle">the angle for calculating isha</param>
        public CalculationParameters(double fajrAngle, double ishaAngle) 
        {
            this.FajrAngle = fajrAngle;
            this.IshaAngle = ishaAngle;
        }

        /// <summary>
        /// Generate CalculationParameters from fajr angle and isha interval
        /// </summary>
        /// <param name="fajrAngle">the angle for calculating fajr</param>
        /// <param name="ishaInterval">the amount of time after maghrib to have isha</param>
        public CalculationParameters(double fajrAngle, int ishaInterval) 
        {
            this.FajrAngle = fajrAngle;
            this.IshaAngle = 0.0;
            this.IshaInterval = ishaInterval;
        }

        /// <summary>
        /// Generate CalculationParameters from angles and a calculation method
        /// </summary>
        /// <param name="fajrAngle">the angle for calculating fajr</param>
        /// <param name="ishaAngle">ishaAngle the angle for calculating isha</param>
        /// <param name="method">the calculation method to use</param>
        public CalculationParameters(double fajrAngle, double ishaAngle, CalculationMethod method) 
        {
            this.FajrAngle = fajrAngle;
            this.IshaAngle = ishaAngle;
            this.Method = method;
        }

        /// <summary>
        /// Generate CalculationParameters from fajr angle, isha interval, and calculation method
        /// </summary>
        /// <param name="fajrAngle">the angle for calculating fajr</param>
        /// <param name="ishaInterval">the amount of time after maghrib to have isha</param>
        /// <param name="method">the calculation method to use</param>
        public CalculationParameters(double fajrAngle, int ishaInterval, CalculationMethod method) 
        {
            this.FajrAngle = fajrAngle;
            this.IshaAngle = 0.0;
            this.IshaInterval = ishaInterval;
            this.Method = method;
        }

        /// <summary>
        /// Set the method adjustments for the current calculation parameters
        /// </summary>
        /// <param name="adjustments">the prayer adjustments</param>
        /// <returns>this calculation parameters instance</returns>
        public CalculationParameters withMethodAdjustments(PrayerAdjustments adjustments) 
        {
            this.MethodAdjustments = adjustments;
            return this;
        }

        public NightPortions NightPortions()
        {
            switch(this.HighLatitudeRule)
            {
                case HighLatitudeRule.MIDDLE_OF_THE_NIGHT:
                    return new NightPortions(1.0 / 2.0, 1.0 / 2.0);
                case HighLatitudeRule.SEVENTH_OF_THE_NIGHT:
                    return new NightPortions(1.0 / 7.0, 1.0 / 7.0);
                case HighLatitudeRule.TWILIGHT_ANGLE:
                    return new NightPortions(this.FajrAngle / 60.0, this.IshaAngle / 60.0);
                default:
                    throw new ArgumentException("Invalid high latitude rule");
            }
        }
    }
}