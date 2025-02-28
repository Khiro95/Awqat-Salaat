using System;

namespace Batoulapps.Adhan.Internal
{
    /// <summary>
    /// Astronomical equations
    /// </summary>
    public class Astronomical 
    {
        /// <summary>
        /// The geometric mean longitude of the sun in degrees.
        /// </summary>
        /// <param name="T">the julian century</param>
        /// <returns>the geometric longitude of the sun in degrees</returns>
        public static double MeanSolarLongitude(double T) 
        {
            /* Equation from Astronomical Algorithms page 163 */
            double term1 = 280.4664567;
            double term2 = 36000.76983 * T;
            double term3 = 0.0003032 * Math.Pow(T, 2);
            double L0 = term1 + term2 + term3;
            return DoubleUtil.UnwindAngle(L0);
        }

        /// <summary>
        /// The geometric mean longitude of the moon in degrees
        /// </summary>
        /// <param name="T">the julian century</param>
        /// <returns>the geometric mean longitude of the moon in degrees</returns>
        public static double MeanLunarLongitude(double T) 
        {
            /* Equation from Astronomical Algorithms page 144 */
            double term1 = 218.3165;
            double term2 = 481267.8813 * T;
            double Lp = term1 + term2;
            return DoubleUtil.UnwindAngle(Lp);
        }

        /// <summary>
        /// The apparent longitude of the Sun, referred to the true equinox of the date.
        /// </summary>
        /// <param name="T">the julian century</param>
        /// <param name="L0">the mean longitude</param>
        /// <returns>the true equinox of the date</returns>
        public static double ApparentSolarLongitude(double T, double L0) 
        {
            /* Equation from Astronomical Algorithms page 164 */
            double longitude = L0 + SolarEquationOfTheCenter(T, MeanSolarAnomaly(T));
            double Ω = 125.04 - (1934.136 * T);
            double λ = longitude - 0.00569 - (0.00478 * Math.Sin(MathHelper.ToRadians(Ω)));
            return DoubleUtil.UnwindAngle(λ);
        }

        /// <summary>
        /// The ascending lunar node longitude
        /// </summary>
        /// <param name="T">the julian century</param>
        /// <returns>the ascending lunar node longitude</returns>
        public static double AscendingLunarNodeLongitude(double T) 
        {
            /* Equation from Astronomical Algorithms page 144 */
            double term1 = 125.04452;
            double term2 = 1934.136261 * T;
            double term3 = 0.0020708 * Math.Pow(T, 2);
            double term4 = Math.Pow(T, 3) / 450000;
            double Ω = term1 - term2 + term3 + term4;
            return DoubleUtil.UnwindAngle(Ω);
        }

        /// <summary>
        /// The mean anomaly of the sun
        /// </summary>
        /// <param name="T">the julian century</param>
        /// <returns>the mean solar anomaly</returns>
        public static double MeanSolarAnomaly(double T) {
            /* Equation from Astronomical Algorithms page 163 */
            double term1 = 357.52911;
            double term2 = 35999.05029 * T;
            double term3 = 0.0001537 * Math.Pow(T, 2);
            double M = term1 + term2 - term3;
            return DoubleUtil.UnwindAngle(M);
        }

        /// <summary>
        /// The Sun's equation of the center in degrees.
        /// </summary>
        /// <param name="T">the julian century</param>
        /// <param name="M">the mean anomaly</param>
        /// <returns>the sun's equation of the center in degrees</returns>
        public static double SolarEquationOfTheCenter(double T, double M) 
        {
            /* Equation from Astronomical Algorithms page 164 */
            double Mrad = MathHelper.ToRadians(M);
            double term1 = (1.914602 - (0.004817 * T) - (0.000014 * Math.Pow(T, 2))) * Math.Sin(Mrad);
            double term2 = (0.019993 - (0.000101 * T)) * Math.Sin(2 * Mrad);
            double term3 = 0.000289 * Math.Sin(3 * Mrad);
            return term1 + term2 + term3;
        }

        /// <summary>
        /// The mean obliquity of the ecliptic in degrees
        /// formula adopted by the International Astronomical Union.
        /// </summary>
        /// <param name="T">the julian century</param>
        /// <returns>the mean obliquity of the ecliptic in degrees</returns>
        public static double MeanObliquityOfTheEcliptic(double T) 
        {
            /* Equation from Astronomical Algorithms page 147 */
            double term1 = 23.439291;
            double term2 = 0.013004167 * T;
            double term3 = 0.0000001639 * Math.Pow(T, 2);
            double term4 = 0.0000005036 * Math.Pow(T, 3);
            return term1 - term2 - term3 + term4;
        }

        /// <summary>
        /// The mean obliquity of the ecliptic, corrected for calculating the
        /// apparent position of the sun, in degrees.
        /// </summary>
        /// <param name="T">the julian century</param>
        /// <param name="ε0">the mean obliquity of the ecliptic</param>
        /// <returns>the corrected mean obliquity of the ecliptic in degrees</returns>
        public static double ApparentObliquityOfTheEcliptic(double T, double ε0) 
        {
            /* Equation from Astronomical Algorithms page 165 */
            double O = 125.04 - (1934.136 * T);
            return ε0 + (0.00256 * Math.Cos(MathHelper.ToRadians(O)));
        }

        /// <summary>
        /// Mean sidereal time, the hour angle of the vernal equinox, in degrees.
        /// </summary>
        /// <param name="T">the julian century</param>
        /// <returns>the mean sidereal time in degrees</returns>
        public static double MeanSiderealTime(double T) 
        {
            /* Equation from Astronomical Algorithms page 165 */
            double JD = (T * 36525) + 2451545.0;
            double term1 = 280.46061837;
            double term2 = 360.98564736629 * (JD - 2451545);
            double term3 = 0.000387933 * Math.Pow(T, 2);
            double term4 = Math.Pow(T, 3) / 38710000;
            double θ = term1 + term2 + term3 - term4;
            return DoubleUtil.UnwindAngle(θ);
        }

        /// <summary>
        /// Get the nutation in longitude
        /// </summary>
        /// <param name="T">the julian century</param>
        /// <param name="L0">the solar longitude</param>
        /// <param name="Lp">the lunar longitude</param>
        /// <param name="Ω">the ascending node</param>
        /// <returns>the nutation in longitude</returns>
        public static double NutationInLongitude(double T, double L0, double Lp, double Ω) 
        {
            /* Equation from Astronomical Algorithms page 144 */
            double term1 = (-17.2/3600) * Math.Sin(MathHelper.ToRadians(Ω));
            double term2 =  (1.32/3600) * Math.Sin(2 * MathHelper.ToRadians(L0));
            double term3 =  (0.23/3600) * Math.Sin(2 * MathHelper.ToRadians(Lp));
            double term4 =  (0.21/3600) * Math.Sin(2 * MathHelper.ToRadians(Ω));
            return term1 - term2 - term3 + term4;
        }

        /// <summary>
        /// Get the nutation in obliquity
        /// </summary>
        /// <param name="T">the julian century</param>
        /// <param name="L0">the solar longitude</param>
        /// <param name="Lp">the lunar longitude</param>
        /// <param name="Ω">the ascending node</param>
        /// <returns>the nutation in obliquity</returns>
        public static double NutationInObliquity(double T, double L0, double Lp, double Ω) 
        {
            /* Equation from Astronomical Algorithms page 144 */
            double term1 =  (9.2/3600) * Math.Cos(MathHelper.ToRadians(Ω));
            double term2 = (0.57/3600) * Math.Cos(2 * MathHelper.ToRadians(L0));
            double term3 = (0.10/3600) * Math.Cos(2 * MathHelper.ToRadians(Lp));
            double term4 = (0.09/3600) * Math.Cos(2 * MathHelper.ToRadians(Ω));
            return term1 + term2 + term3 - term4;
        }

        /// <summary>
        /// Return the altitude of the celestial body
        /// </summary>
        /// <param name="φ">the observer latitude</param>
        /// <param name="δ">the declination</param>
        /// <param name="H">the local hour angle</param>
        /// <returns>the altitude of the celestial body</returns>
        public static double AltitudeOfCelestialBody(double φ, double δ, double H) 
        {
            /* Equation from Astronomical Algorithms page 93 */
            double term1 = Math.Sin(MathHelper.ToRadians(φ)) * Math.Sin(MathHelper.ToRadians(δ));
            double term2 = Math.Cos(MathHelper.ToRadians(φ)) *
                Math.Cos(MathHelper.ToRadians(δ)) * Math.Cos(MathHelper.ToRadians(H));
            return MathHelper.ToDegrees(Math.Asin(term1 + term2));
        }

        /// <summary>
        /// Return the approximate transite
        /// </summary>
        /// <param name="L">the longitude</param>
        /// <param name="Θ0">the sidereal time</param>
        /// <param name="α2">the right ascension</param>
        /// <returns>the approximate transite</returns>
        public static double ApproximateTransit(double L, double Θ0, double α2) 
        {
            /* Equation from page Astronomical Algorithms 102 */
            double Lw = L * -1;
            return DoubleUtil.NormalizeWithBound((α2 + Lw - Θ0) / 360, 1);
        }

        /// <summary>
        /// The time at which the sun is at its highest point in the sky (in universal time)
        /// </summary>
        /// <param name="m0">approximate transit</param>
        /// <param name="L">the longitude</param>
        /// <param name="Θ0">the sidereal time</param>
        /// <param name="α2">the right ascension</param>
        /// <param name="α1">the previous right ascension</param>
        /// <param name="α3">the next right ascension</param>
        /// <returns>the time (in universal time) when the sun is at its highest point in the sky</returns>
        public static double CorrectedTransit(double m0, double L, double Θ0, double α2, double α1, double α3) 
        {
                /* Equation from page Astronomical Algorithms 102 */
            double Lw = L * -1;
            double θ = DoubleUtil.UnwindAngle(Θ0 + (360.985647 * m0));
            double α = DoubleUtil.UnwindAngle(InterpolateAngles(
                    /* value */ α2, /* previousValue */ α1, /* nextValue */ α3, /* factor */ m0));
            double H = DoubleUtil.ClosestAngle(θ - Lw - α);
            double Δm = H / -360;
            return (m0 + Δm) * 24;
        }

        /// <summary>
        /// Get the corrected hour angle
        /// </summary>
        /// <param name="m0">the approximate transit</param>
        /// <param name="h0">the angle</param>
        /// <param name="coordinates">the coordinates</param>
        /// <param name="afterTransit">whether it's after transit</param>
        /// <param name="Θ0">the sidereal time</param>
        /// <param name="α2">the right ascension</param>
        /// <param name="α1">the previous right ascension</param>
        /// <param name="α3">the next right ascension</param>
        /// <param name="δ2">the declination</param>
        /// <param name="δ1">the previous declination</param>
        /// <param name="δ3">the next declination</param>
        /// <returns>the corrected hour angle</returns>
        public static double CorrectedHourAngle(double m0, double h0, Coordinates coordinates, bool afterTransit,
            double Θ0, double α2, double α1, double α3, double δ2, double δ1, double δ3) 
            {
            /* Equation from page Astronomical Algorithms 102 */
            double Lw = coordinates.Longitude * -1;
            double term1 = Math.Sin(MathHelper.ToRadians(h0)) -
                (Math.Sin(MathHelper.ToRadians(coordinates.Latitude)) * Math.Sin(MathHelper.ToRadians(δ2)));
            double term2 = Math.Cos(MathHelper.ToRadians(coordinates.Latitude)) * Math.Cos(MathHelper.ToRadians(δ2));
            double H0 = MathHelper.ToDegrees(Math.Acos(term1 / term2));
            double m = afterTransit ? m0 + (H0 / 360) : m0 - (H0 / 360);
            double θ = DoubleUtil.UnwindAngle(Θ0 + (360.985647 * m));
            double α = DoubleUtil.UnwindAngle(InterpolateAngles(
                /* value */ α2, /* previousValue */ α1, /* nextValue */ α3, /* factor */ m));
            double δ = Interpolate(/* value */ δ2, /* previousValue */ δ1,
                /* nextValue */ δ3, /* factor */ m);
            double H = (θ - Lw - α);
            double h = AltitudeOfCelestialBody(/* observerLatitude */ coordinates.Latitude,
                /* declination */ δ, /* localHourAngle */ H);
            double term3 = h - h0;
            double term4 = 360 * Math.Cos(MathHelper.ToRadians(δ)) *
                Math.Cos(MathHelper.ToRadians(coordinates.Latitude)) * Math.Sin(MathHelper.ToRadians(H));
            double Δm = term3 / term4;
            return (m + Δm) * 24;
        }

        /// <summary>
        /// Interpolation of a value given equidistant
        /// previous and next values and a factor
        /// equal to the fraction of the interpolated
        /// point's time over the time between values.
        /// </summary>
        /// <param name="y2">the value</param>
        /// <param name="y1">the previous value</param>
        /// <param name="y3">the next value</param>
        /// <param name="n">the factor</param>
        /// <returns>the interpolated value</returns>
        public static double Interpolate(double y2, double y1, double y3, double n) 
        {
            /* Equation from Astronomical Algorithms page 24 */
            double a = y2 - y1;
            double b = y3 - y2;
            double c = b - a;
            return y2 + ((n/2) * (a + b + (n * c)));
        }

        /// <summary>
        /// Interpolation of three angles, accounting for angle unwinding
        /// </summary>
        /// <param name="y2">value</param>
        /// <param name="y1">previousValue</param>
        /// <param name="y3">nextValue</param>
        /// <param name="n">factor</param>
        /// <returns>interpolated angle</returns>
        public static double InterpolateAngles(double y2, double y1, double y3, double n) 
        {
            /* Equation from Astronomical Algorithms page 24 */
            double a = DoubleUtil.UnwindAngle(y2 - y1);
            double b = DoubleUtil.UnwindAngle(y3 - y2);
            double c = b - a;
            return y2 + ((n/2) * (a + b + (n * c)));
        }
    }
}