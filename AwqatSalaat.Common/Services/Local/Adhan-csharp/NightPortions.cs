using System;

namespace Batoulapps.Adhan
{
    public class NightPortions 
    {
        public readonly double Fajr;
        public readonly double Isha;

        public NightPortions(double fajr, double isha) 
        {
            this.Fajr = fajr;
            this.Isha = isha;
        }
    }
}