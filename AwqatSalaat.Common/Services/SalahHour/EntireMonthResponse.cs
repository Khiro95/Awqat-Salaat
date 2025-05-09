﻿using AwqatSalaat.Data;
using System;
using System.Collections.Generic;

namespace AwqatSalaat.Services.SalahHour
{
    public class EntireMonthResponse : Response
    {
        public Dictionary<string, Dictionary<string, string>> Results { get; set; }
        public Dictionary<DateTime, PrayerTimes> Times { get; set; }

        protected sealed override void ParseImpl()
        {
            Times = new Dictionary<DateTime, PrayerTimes>(Results.Count);

            foreach (var day in Results)
            {
                DateTime date = DateTime.Parse(day.Key, System.Globalization.CultureInfo.InvariantCulture);
                Dictionary<string, DateTime> dayTimes = new Dictionary<string, DateTime>(5);

                foreach (var time in day.Value)
                {
                    if (time.Key == "Duha")
                    {
                        dayTimes.Add(nameof(PrayerTimes.Shuruq), ParseTimeToLocal(time.Value, date));
                    }
                    else
                    {
                        dayTimes.Add(time.Key, ParseTimeToLocal(time.Value, date));
                    }
                }

                Times.Add(date, new PrayerTimes(dayTimes));
            }
        }
    }
}
