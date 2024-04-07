using System;
using System.Runtime.Serialization;

namespace AwqatSalaat.Services.IslamicFinder
{
    public abstract class Response
    {
        protected TimeZoneInfo timeZone;

        public bool Success { get; set; }
        public string Message { get; set; }
        public Settings Settings { get; set; }

        [OnDeserialized]
        private void Parse(StreamingContext context)
        {
            if (Success)
            {
                if (TimeZoneConverter.TZConvert.TryIanaToWindows(Settings.TimeZone, out string tzID))
                {
                    timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzID);
                }

                ParseImpl();
            }
        }

        protected abstract void ParseImpl();

        protected DateTime ParseTimeToLocal(string time, DateTime baseDate)
        {
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
