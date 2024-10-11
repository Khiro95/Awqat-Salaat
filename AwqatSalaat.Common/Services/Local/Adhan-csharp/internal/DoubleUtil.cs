using System;

namespace Batoulapps.Adhan.Internal
{
    public class DoubleUtil 
    {
        public static double NormalizeWithBound(double value, double max) 
        {
            return value - (max * (Math.Floor(value / max)));
        }

        public static double UnwindAngle(double value) 
        {
            return NormalizeWithBound(value, 360);
        }

        public static double ClosestAngle(double angle) 
        {
            if (angle >= -180 && angle <= 180) {
            return angle;
            }
            return angle - (360 * Math.Round(angle / 360));
        }
    }
}