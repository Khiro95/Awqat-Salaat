using AwqatSalaat.Helpers;
using System;
using System.Runtime.Serialization;

namespace AwqatSalaat.Services.AlAdhan
{
    internal abstract class DataResponse<T> : Response<T> where T : class
    {
        protected abstract string ResponseTimeZone { get; }

        [OnDeserialized]
        private void Parse(StreamingContext context)
        {
            if (Code == 200)
            {
                ParseImpl();
            }
        }

        protected abstract void ParseImpl();

        protected DateTime ParseTimeToLocal(string time, DateTime baseDate)
        {
            time = time.Split(' ')[0];
            var dt = baseDate + DateTime.Parse(time, System.Globalization.CultureInfo.InvariantCulture).TimeOfDay;

            return TimeZoneHelper.ConvertDateTimeToLocal(dt, ResponseTimeZone);
        }
    }
}
