using System;

namespace Batoulapps.Adhan.Internal
{
    public class SolarTime 
    {
        public readonly double Transit;
        public readonly double Sunrise;
        public readonly double Sunset;

        private readonly Coordinates observer;
        private readonly SolarCoordinates solar;
        private readonly SolarCoordinates prevSolar;
        private readonly SolarCoordinates nextSolar;
        private double approximateTransit;

        public SolarTime(DateTime today, Coordinates coordinates) 
        {
            DateTime calendar = today.ToUniversalTime();
            DateTime tomorrow = calendar.AddDays(1);
            DateTime yesterday = calendar.AddDays(-1);

            this.prevSolar = new SolarCoordinates(CalendricalHelper.JulianDay(yesterday));
            this.solar = new SolarCoordinates(CalendricalHelper.JulianDay(today));
            this.nextSolar = new SolarCoordinates(CalendricalHelper.JulianDay(tomorrow));

            this.approximateTransit = Astronomical.ApproximateTransit(coordinates.Longitude,
                solar.ApparentSiderealTime, solar.RightAscension);
            double solarAltitude = -50.0 / 60.0;

            this.observer = coordinates;
            this.Transit = Astronomical.CorrectedTransit(this.approximateTransit, coordinates.Longitude,
                solar.ApparentSiderealTime, solar.RightAscension, prevSolar.RightAscension,
                nextSolar.RightAscension);
            this.Sunrise = Astronomical.CorrectedHourAngle(this.approximateTransit, solarAltitude,
                coordinates, false, solar.ApparentSiderealTime, solar.RightAscension,
                prevSolar.RightAscension, nextSolar.RightAscension, solar.Declination,
                prevSolar.Declination, nextSolar.Declination);
            this.Sunset = Astronomical.CorrectedHourAngle(this.approximateTransit, solarAltitude,
                coordinates, true, solar.ApparentSiderealTime, solar.RightAscension,
                prevSolar.RightAscension, nextSolar.RightAscension, solar.Declination,
                prevSolar.Declination, nextSolar.Declination);
        }

        public double HourAngle(double angle, bool afterTransit) 
        {
            return Astronomical.CorrectedHourAngle(this.approximateTransit, angle, this.observer,
                afterTransit, this.solar.ApparentSiderealTime, this.solar.RightAscension,
                this.prevSolar.RightAscension, this.nextSolar.RightAscension, this.solar.Declination,
                this.prevSolar.Declination, this.nextSolar.Declination);
        }

        public double Afternoon(ShadowLength shadowLength) 
        {
            // TODO (from Swift version) source shadow angle calculation
            double tangent = Math.Abs(observer.Latitude - solar.Declination);
            double inverse = (double) shadowLength + Math.Tan(MathHelper.ToRadians(tangent));
            double angle = MathHelper.ToDegrees(Math.Atan(1.0 / inverse));

            return HourAngle(angle, true);
        }
    }
}