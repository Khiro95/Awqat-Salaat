using System;
using Batoulapps.Adhan.Internal;

namespace Batoulapps.Adhan
{
    public class PrayerTimes 
    {
        public readonly DateTime Fajr;
        public readonly DateTime Sunrise;
        public readonly DateTime Dhuhr;
        public readonly DateTime Asr;
        public readonly DateTime Maghrib;
        public readonly DateTime Isha;

        /// <summary>
        /// Calculate PrayerTimes
        /// </summary>
        /// <param name="coordinates">the coordinates of the location</param>
        /// <param name="date">the date components for that location</param>
        /// <param name="params">he parameters for the calculation</param>
        public PrayerTimes(Coordinates coordinates, DateComponents date, CalculationParameters parameters)
            : this (coordinates, CalendarUtil.ResolveTime(date), parameters)
        {}

        private PrayerTimes(Coordinates coordinates, DateTime date, CalculationParameters parameters) 
        {
            DateTime? tempFajr = null;
            DateTime? tempSunrise = null;
            DateTime? tempDhuhr = null;
            DateTime? tempAsr = null;
            DateTime? tempMaghrib = null;
            DateTime? tempIsha = null;

            //DateTime calendar = date.ToUniversalTime();
            int year = date.Year;
            int dayOfYear = date.DayOfYear;

            SolarTime solarTime = new SolarTime(date, coordinates);

            TimeComponents timeComponents = TimeComponents.FromDouble(solarTime.Transit);
            DateTime? transit = timeComponents?.DateComponents(date);

            timeComponents = TimeComponents.FromDouble(solarTime.Sunrise);
            DateTime? sunriseComponents = timeComponents?.DateComponents(date);

            timeComponents = TimeComponents.FromDouble(solarTime.Sunset);
            DateTime? sunsetComponents = timeComponents?.DateComponents(date);

            bool error = transit == null || sunriseComponents == null || sunsetComponents == null;
    
            if (!error) 
            {
                tempDhuhr = transit;
                tempSunrise = sunriseComponents;
                tempMaghrib = sunsetComponents;

                timeComponents = TimeComponents.FromDouble(
                    solarTime.Afternoon(parameters.Madhab.GetShadowLength()));

                if (timeComponents != null) 
                {
                    tempAsr = timeComponents.DateComponents(date);
                }

                // get night length
                DateTime tomorrowSunrise = sunriseComponents.Value.AddDays(1); 
                double night = tomorrowSunrise.GetTime() - sunsetComponents.Value.GetTime();

                timeComponents = TimeComponents
                    .FromDouble(solarTime.HourAngle(-parameters.FajrAngle, false));
                
                if (timeComponents != null) 
                {
                    tempFajr = timeComponents.DateComponents(date);
                }

                if (parameters.Method == CalculationMethod.MOON_SIGHTING_COMMITTEE &&
                    coordinates.Latitude >= 55) 
                {
                    tempFajr = sunriseComponents.Value.AddSeconds(-1 * (int) (night / 7000));
                }

                NightPortions nightPortions = parameters.NightPortions();

                DateTime safeFajr;
                if (parameters.Method == CalculationMethod.MOON_SIGHTING_COMMITTEE) 
                {
                    safeFajr = SeasonAdjustedMorningTwilight(coordinates.Latitude, dayOfYear, year, sunriseComponents.Value);
                } 
                else 
                {
                    double portion = nightPortions.Fajr;
                    long nightFraction = (long) (portion * night / 1000);
                    safeFajr = sunriseComponents.Value.AddSeconds(-1 * (int) nightFraction);
                }
                
                if (tempFajr == null || tempFajr.Value.Before(safeFajr)) {
                    tempFajr = safeFajr;
                }

                // Isha calculation with check against safe value
                if (parameters.IshaInterval > 0) 
                {
                    tempIsha = tempMaghrib.Value.AddSeconds(parameters.IshaInterval * 60);
                } 
                else 
                {
                    timeComponents = TimeComponents.FromDouble(
                        solarTime.HourAngle(-parameters.IshaAngle, true));

                    if (timeComponents != null) 
                    {
                        tempIsha = timeComponents.DateComponents(date);
                    }

                    if (parameters.Method == CalculationMethod.MOON_SIGHTING_COMMITTEE &&
                        coordinates.Latitude >= 55) 
                    {
                        long nightFraction = (long) night / 7000;
                        tempIsha = sunsetComponents.Value.AddSeconds(nightFraction);
                    }

                    DateTime safeIsha;
                    if (parameters.Method == CalculationMethod.MOON_SIGHTING_COMMITTEE) 
                    {
                        safeIsha = PrayerTimes.SeasonAdjustedEveningTwilight(
                            coordinates.Latitude, dayOfYear, year, sunsetComponents.Value);
                    } 
                    else 
                    {
                        double portion = nightPortions.Isha;
                        long nightFraction = (long) (portion * night / 1000);
                        safeIsha = sunsetComponents.Value.AddSeconds(nightFraction);
                    }

                    if (tempIsha == null || (tempIsha.Value.After(safeIsha))) 
                    {
                        tempIsha = safeIsha;
                    }
                }
            }

            if (error || tempAsr == null) 
            {
                // if we don't have all prayer times then initialization failed
                this.Fajr = DateTime.MinValue;
                this.Sunrise = DateTime.MinValue;
                this.Dhuhr = DateTime.MinValue;
                this.Asr = DateTime.MinValue;
                this.Maghrib = DateTime.MinValue;
                this.Isha = DateTime.MinValue;
            } 
            else 
            {
                // Assign final times to public struct members with all offsets
                this.Fajr = CalendarUtil.RoundedMinute(tempFajr.Value.AddMinutes(parameters.Adjustments.Fajr + parameters.MethodAdjustments.Fajr));
                this.Sunrise = CalendarUtil.RoundedMinute(tempSunrise.Value.AddMinutes(parameters.Adjustments.Sunrise + parameters.MethodAdjustments.Sunrise));
                this.Dhuhr = CalendarUtil.RoundedMinute(tempDhuhr.Value.AddMinutes(parameters.Adjustments.Dhuhr + parameters.MethodAdjustments.Dhuhr));
                this.Asr = CalendarUtil.RoundedMinute(tempAsr.Value.AddMinutes(parameters.Adjustments.Asr + parameters.MethodAdjustments.Asr));
                this.Maghrib = CalendarUtil.RoundedMinute(tempMaghrib.Value.AddMinutes(parameters.Adjustments.Maghrib + parameters.MethodAdjustments.Maghrib));
                this.Isha = CalendarUtil.RoundedMinute(tempIsha.Value.AddMinutes(parameters.Adjustments.Isha + parameters.MethodAdjustments.Isha));                
            }
        }
        
        public Prayer CurrentPrayer() 
        {
            return CurrentPrayer(new DateTime());
        }

        public Prayer CurrentPrayer(DateTime time) 
        {
            double when = time.GetTime();

            if (this.Isha.GetTime() - when <= 0) 
            {
                return Prayer.ISHA;
            } 
            else if (this.Maghrib.GetTime() - when <= 0) 
            {
                return Prayer.MAGHRIB;
            } 
            else if (this.Asr.GetTime() - when <= 0) 
            {
                return Prayer.ASR;
            } 
            else if (this.Dhuhr.GetTime() - when <= 0) 
            {
                return Prayer.DHUHR;
            } 
            else if (this.Sunrise.GetTime() - when <= 0) 
            {
                return Prayer.SUNRISE;
            } 
            else if (this.Fajr.GetTime() - when <= 0) 
            {
                return Prayer.FAJR;
            } 
            else 
            {
                return Prayer.NONE;
            }
        }

        public Prayer NextPrayer() 
        {
            return NextPrayer(new DateTime());
        }

        public Prayer NextPrayer(DateTime time) 
        {
            double when = time.GetTime();                 

            if (this.Isha.GetTime() - when <= 0) 
            {
                return Prayer.NONE;
            } 
            else if (this.Maghrib.GetTime() - when <= 0) 
            {
                return Prayer.ISHA;
            } 
            else if (this.Asr.GetTime() - when <= 0) 
            {
                return Prayer.MAGHRIB;
            } 
            else if (this.Dhuhr.GetTime() - when <= 0) 
            {
                return Prayer.ASR;
            } 
            else if (this.Sunrise.GetTime() - when <= 0) 
            {
                return Prayer.DHUHR;
            } 
            else if (this.Fajr.GetTime() - when <= 0) 
            {
                return Prayer.SUNRISE;
            } 
            else 
            {
                return Prayer.FAJR;
            }
        }

        public DateTime? TimeForPrayer(Prayer prayer) 
        {
            switch (prayer) {
            case Prayer.FAJR:
                return this.Fajr;
            case Prayer.SUNRISE:
                return this.Sunrise;
            case Prayer.DHUHR:
                return this.Dhuhr;
            case Prayer.ASR:
                return this.Asr;
            case Prayer.MAGHRIB:
                return this.Maghrib;
            case Prayer.ISHA:
                return this.Isha;
            case Prayer.NONE:
            default:
                    return null;
            }
        }

        private static DateTime SeasonAdjustedMorningTwilight(
            double latitude, int day, int year, DateTime sunrise) 
        {
            double a = 75 + ((28.65 / 55.0) * Math.Abs(latitude));
            double b = 75 + ((19.44 / 55.0) * Math.Abs(latitude));
            double c = 75 + ((32.74 / 55.0) * Math.Abs(latitude));
            double d = 75 + ((48.10 / 55.0) * Math.Abs(latitude));

            double adjustment;
            int dyy = PrayerTimes.DaysSinceSolstice(day, year, latitude);
            if ( dyy < 91) 
            {
                adjustment = a + ( b - a ) / 91.0 * dyy;
            } 
            else if ( dyy < 137) 
            {
                adjustment = b + ( c - b ) / 46.0 * ( dyy - 91 );
            } 
            else if ( dyy < 183 ) 
            {
                adjustment = c + ( d - c ) / 46.0 * ( dyy - 137 );
            } 
            else if ( dyy < 229 ) 
            {
                adjustment = d + ( c - d ) / 46.0 * ( dyy - 183 );
            } 
            else if ( dyy < 275 ) 
            {
                adjustment = c + ( b - c ) / 46.0 * ( dyy - 229 );
            } else 
            {
                adjustment = b + ( a - b ) / 91.0 * ( dyy - 275 );
            }

            return sunrise.AddSeconds(-(int) Math.Round(adjustment * 60.0));
        }

        private static DateTime SeasonAdjustedEveningTwilight(
            double latitude, int day, int year, DateTime sunset) 
        {
            double a = 75 + ((25.60 / 55.0) * Math.Abs(latitude));
            double b = 75 + ((2.050 / 55.0) * Math.Abs(latitude));
            double c = 75 - ((9.210 / 55.0) * Math.Abs(latitude));
            double d = 75 + ((6.140 / 55.0) * Math.Abs(latitude));

            double adjustment;
            int dyy = PrayerTimes.DaysSinceSolstice(day, year, latitude);
            if ( dyy < 91) 
            {
                adjustment = a + ( b - a ) / 91.0 * dyy;
            } 
            else if ( dyy < 137) 
            {
                adjustment = b + ( c - b ) / 46.0 * ( dyy - 91 );
            } 
            else if ( dyy < 183 ) 
            {
                adjustment = c + ( d - c ) / 46.0 * ( dyy - 137 );
            } 
            else if ( dyy < 229 ) 
            {
                adjustment = d + ( c - d ) / 46.0 * ( dyy - 183 );
            } 
            else if ( dyy < 275 ) 
            {
                adjustment = c + ( b - c ) / 46.0 * ( dyy - 229 );
            } 
            else 
            {
                adjustment = b + ( a - b ) / 91.0 * ( dyy - 275 );
            }

            return sunset.AddSeconds((int) Math.Round(adjustment * 60.0));
        }

        public static int DaysSinceSolstice(int dayOfYear, int year, double latitude) 
        {
            int daysSinceSolistice;
            int northernOffset = 10;
            bool isLeapYear = DateTime.IsLeapYear(year);
            int southernOffset = isLeapYear ? 173 : 172;
            int daysInYear = isLeapYear ? 366 : 365;

            if (latitude >= 0) 
            {
                daysSinceSolistice = dayOfYear + northernOffset;
                if (daysSinceSolistice >= daysInYear) 
                {
                    daysSinceSolistice = daysSinceSolistice - daysInYear;
                }
            } 
            else 
            {
                daysSinceSolistice = dayOfYear - southernOffset;
                if (daysSinceSolistice < 0) 
                {
                    daysSinceSolistice = daysSinceSolistice + daysInYear;
                }
            }

            return daysSinceSolistice;
        }
    }
}