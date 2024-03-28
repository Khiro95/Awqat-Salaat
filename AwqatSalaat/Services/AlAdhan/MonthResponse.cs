using AwqatSalaat.Data;
using System;
using System.Collections.Generic;

namespace AwqatSalaat.Services.AlAdhan
{
    internal class MonthResponse : DataResponse<DayData[]>
    {
        protected override string ResponseTimeZone => Data[0].Meta.TimeZone;

        public Dictionary<DateTime, PrayerTimes> Times { get; private set; }

        protected override void ParseImpl()
        {
            Times = new Dictionary<DateTime, PrayerTimes>(Data.Length);

            foreach (var day in Data)
            {
                DateTime date = day.Date.ToDateTime();
                Dictionary<string, DateTime> dayTimes = new Dictionary<string, DateTime>(5);

                foreach (var time in day.Timings)
                {
                    dayTimes.Add(time.Key, ParseTimeToLocal(time.Value, date));
                }

                Times.Add(date, new PrayerTimes(dayTimes));
            }
        }
    }
}
