using System;
using System.Collections.Generic;
using AwqatSalaat.Data;

namespace AwqatSalaat.Services
{
    public class ServiceData
    {
        public Dictionary<DateTime, PrayerTimes> Times { get; set; }
        public Location Location { get; set; }
    }
}