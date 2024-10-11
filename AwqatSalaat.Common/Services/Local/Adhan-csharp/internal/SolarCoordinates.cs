using System;

namespace Batoulapps.Adhan.Internal
{
    public class SolarCoordinates 
    {
        /// <summary>
        /// The declination of the sun, the angle between
        /// the rays of the Sun and the plane of the Earth's
        /// equator, in degrees.
        /// </summary>
        public readonly double Declination;

        /// <summary>
        /// Right ascension of the Sun, the angular distance on the
        /// celestial equator from the vernal equinox to the hour circle,
        /// in degrees.
        /// </summary>
        public readonly double RightAscension;

        /// <summary>
        /// Apparent sidereal time, the hour angle of the vernal
        /// equinox, in degrees.
        /// </summary>
        public readonly double ApparentSiderealTime;

        public SolarCoordinates(double julianDay) 
        {
            double T = CalendricalHelper.JulianCentury(julianDay);
            double L0 = Astronomical.MeanSolarLongitude(/* julianCentury */ T);
            double Lp = Astronomical.MeanLunarLongitude(/* julianCentury */ T);
            double Ω = Astronomical.AscendingLunarNodeLongitude(/* julianCentury */ T);
            double λ = MathHelper.ToRadians(
                Astronomical.ApparentSolarLongitude(/* julianCentury*/ T, /* meanLongitude */ L0));

            double θ0 = Astronomical.MeanSiderealTime(/* julianCentury */ T);
            double ΔΨ = Astronomical.NutationInLongitude(/* julianCentury */ T, /* solarLongitude */ L0,
                /* lunarLongitude */ Lp, /* ascendingNode */ Ω);
            double Δε = Astronomical.NutationInObliquity(/* julianCentury */ T, /* solarLongitude */ L0,
                /* lunarLongitude */ Lp, /* ascendingNode */ Ω);

            double ε0 = Astronomical.MeanObliquityOfTheEcliptic(/* julianCentury */ T);
            double εapp = MathHelper.ToRadians(Astronomical.ApparentObliquityOfTheEcliptic(
                /* julianCentury */ T, /* meanObliquityOfTheEcliptic */ ε0));

                /* Equation from Astronomical Algorithms page 165 */
            this.Declination = MathHelper.ToDegrees(Math.Asin(Math.Sin(εapp) * Math.Sin(λ)));

                /* Equation from Astronomical Algorithms page 165 */
            this.RightAscension = DoubleUtil.UnwindAngle(
                MathHelper.ToDegrees(Math.Atan2(Math.Cos(εapp) * Math.Sin(λ), Math.Cos(λ))));

                /* Equation from Astronomical Algorithms page 88 */
            this.ApparentSiderealTime = θ0 + (((ΔΨ * 3600) * Math.Cos(MathHelper.ToRadians(ε0 + Δε))) / 3600);
        }
    }
}
