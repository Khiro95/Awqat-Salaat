using System;

namespace Batoulapps.Adhan.Internal
{
    public class MathHelper
    {
        public static double ToDegrees(double radians)
        {
            // This method uses double precission internally,
            // though it returns single float
            // Factor = 180 / pi
            return (double)(radians * 57.295779513082320876798154814105);
        }

        public static double ToRadians(double degrees)
        {
            // This method uses double precission internally,
            // though it returns single float
            // Factor = pi / 180
            return (double)(degrees * 0.017453292519943295769236907684886);
        }
    }
}