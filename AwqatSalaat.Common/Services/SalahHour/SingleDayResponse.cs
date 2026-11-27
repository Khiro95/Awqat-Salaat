using AwqatSalaat.Data;
using System;
using System.Collections.Generic;

namespace AwqatSalaat.Services.SalahHour
{
    internal class SingleDayResponse : Response
    {
        public Dictionary<string, string> Results { get; set; }
        public PrayerTimes Times { get; set; }

        protected sealed override void ParseImpl(DateTime requestDate)
        {
            DateTime date = new DateTime(requestDate.Year, requestDate.Month, requestDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
            Dictionary<string, DateTime> dayTimes = new Dictionary<string, DateTime>(5);

            foreach (var time in Results)
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

            Times = new PrayerTimes(dayTimes);
        }
    }
}
