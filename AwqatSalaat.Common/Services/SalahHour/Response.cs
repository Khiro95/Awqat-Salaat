using AwqatSalaat.Helpers;
using System;
using System.Runtime.Serialization;

namespace AwqatSalaat.Services.SalahHour
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
                // The client passes the requested date in the context
                ParseImpl(context.Context as DateTime? ?? TimeStamp.Date);
            }
        }

        protected abstract void ParseImpl(DateTime requestDate);

        protected DateTime ParseTimeToLocal(string time, DateTime baseDate)
        {
            var dt = baseDate + DateTime.Parse(time, System.Globalization.CultureInfo.InvariantCulture).TimeOfDay;

            return TimeZoneHelper.ConvertDateTimeToLocal(dt, Settings.TimeZone);
        }
    }
}
