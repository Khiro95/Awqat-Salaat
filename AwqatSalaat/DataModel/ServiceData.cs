using System;
using System.Collections.Generic;

namespace AwqatSalaat.DataModel
{
    public class ServiceData
    {
        public Dictionary<DateTime, PrayerTimes> Times { get; set; }
        public Location Location { get; set; }
    }
}