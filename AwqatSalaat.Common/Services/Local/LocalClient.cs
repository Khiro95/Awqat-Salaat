using Batoulapps.Adhan;
using Batoulapps.Adhan.Internal;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AwqatSalaat.Services.Local
{
    public class LocalClient : IServiceClient
    {
        public Task<ServiceData> GetDataAsync(IRequest request)
        {
            var req = (LocalRequest)request;
            Log.Debug("[Local] Getting data for request: {@request}", req);

            if (!req.UseCoordinates || req.Latitude == default && req.Longitude == default)
            {
                throw new ArgumentException("Invalid request. Local service require coordinates.");
            }

            try
            {
                var coordinates = new Coordinates((double)req.Latitude, (double)req.Longitude);

                CalculationParameters parameters = req.GetParameters();

                var tzinfo = TimeZoneInfo.Local;

                ServiceData data = new ServiceData();

                if (req.GetEntireMonth)
                {
                    data.Times = GetPrayerTimesForMonth(req, coordinates, parameters, tzinfo);
                }
                else
                {
                    var times = GetPrayerTimesForDate(req.Date, coordinates, parameters, tzinfo);

                    data.Times = new Dictionary<DateTime, Data.PrayerTimes> { [req.Date] = times };
                }

                return Task.FromResult(data);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static Dictionary<DateTime, Data.PrayerTimes> GetPrayerTimesForMonth(LocalRequest req, Coordinates coordinates, CalculationParameters parameters, TimeZoneInfo timeZone)
        {
            var dict = new Dictionary<DateTime, Data.PrayerTimes>();

            foreach (DateTime current in req.GetDates())
            {
                dict.Add(current, GetPrayerTimesForDate(current, coordinates, parameters, timeZone));
            }

            return dict;
        }

        private static Data.PrayerTimes GetPrayerTimesForDate(DateTime date, Coordinates coordinates, CalculationParameters parameters, TimeZoneInfo timeZone)
        {
            var dateComponents = DateComponents.From(date);
            var prayerTimes = new PrayerTimes(coordinates, dateComponents, parameters);
            var times = new Data.PrayerTimes(new Dictionary<string, DateTime>
            {
                [nameof(Data.PrayerTimes.Fajr)]    = TimeZoneInfo.ConvertTimeFromUtc(prayerTimes.Fajr, timeZone),
                [nameof(Data.PrayerTimes.Shuruq)]  = TimeZoneInfo.ConvertTimeFromUtc(prayerTimes.Sunrise, timeZone),
                [nameof(Data.PrayerTimes.Dhuhr)]   = TimeZoneInfo.ConvertTimeFromUtc(prayerTimes.Dhuhr, timeZone),
                [nameof(Data.PrayerTimes.Asr)]     = TimeZoneInfo.ConvertTimeFromUtc(prayerTimes.Asr, timeZone),
                [nameof(Data.PrayerTimes.Maghrib)] = TimeZoneInfo.ConvertTimeFromUtc(prayerTimes.Maghrib, timeZone),
                [nameof(Data.PrayerTimes.Isha)]    = TimeZoneInfo.ConvertTimeFromUtc(prayerTimes.Isha, timeZone),
            });

            return times;
        }
    }
}
