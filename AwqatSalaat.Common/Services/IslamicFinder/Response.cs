using AwqatSalaat.Helpers;
using System;
using System.Runtime.Serialization;

namespace AwqatSalaat.Services.IslamicFinder
{
    public abstract class Response
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Settings Settings { get; set; }

        [OnDeserialized]
        private void Parse(StreamingContext context)
        {
            if (Success)
            {
                ParseImpl();
            }
        }

        protected abstract void ParseImpl();

        protected DateTime ParseTimeToLocal(string time, DateTime baseDate)
        {
            var dt = baseDate + DateTime.Parse(time, System.Globalization.CultureInfo.InvariantCulture).TimeOfDay;

            return TimeZoneHelper.ConvertDateTimeToLocal(dt, Settings.TimeZone);
        }
    }
}
