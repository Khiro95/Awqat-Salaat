using System;
using System.Runtime.Serialization;

namespace AwqatSalaat.Services.AlAdhan
{
    internal abstract class DataResponse<T> : Response<T> where T : class
    {
        protected TimeZoneInfo timeZone;
        protected abstract string ResponseTimeZone { get; }

        [OnDeserialized]
        private void Parse(StreamingContext context)
        {
            if (Code == 200)
            {
                if (TimeZoneConverter.TZConvert.TryIanaToWindows(ResponseTimeZone, out string tzID))
                {
                    timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzID);
                }

                ParseImpl();
            }
        }

        protected abstract void ParseImpl();

        protected DateTime ParseTimeToLocal(string time, DateTime baseDate)
        {
            time = time.Split(' ')[0];
            var dt = baseDate + DateTime.Parse(time, System.Globalization.CultureInfo.InvariantCulture).TimeOfDay;

            if (timeZone != null && timeZone.BaseUtcOffset != TimeZoneInfo.Local.BaseUtcOffset)
            {
                dt = TimeZoneInfo.ConvertTimeToUtc(dt, timeZone);
                dt = TimeZoneInfo.ConvertTimeFromUtc(dt, TimeZoneInfo.Local);
            }

            return dt;
        }
    }
}
