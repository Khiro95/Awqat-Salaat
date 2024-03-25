using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace AwqatSalaat.DataModel.AlAdhanApi
{
    public class Times
    {
        public string Fajr { get; set; }
        public string Dhuhr { get; set; }
        public string Asr { get; set; }
        public string Maghrib { get; set; }
        public string Isha { get; set; }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            yield return new KeyValuePair<string, string>(nameof(Fajr), Fajr);
            yield return new KeyValuePair<string, string>(nameof(Dhuhr), Dhuhr);
            yield return new KeyValuePair<string, string>(nameof(Asr), Asr);
            yield return new KeyValuePair<string, string>(nameof(Maghrib), Maghrib);
            yield return new KeyValuePair<string, string>(nameof(Isha), Isha);
        }
    }

    public class Meta
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string TimeZone { get; set; }
    }

    public class DateInfo
    {
        public string Date { get; set; }
        public string Format { get; set; }
    }

    public class Date
    {
        public string Readable { get; set; }
        public string Timestamp { get; set; }
        public DateInfo Gregorian { get; set; }
        public DateInfo Hijri { get; set; }

        public DateTime ToDateTime()
            => DateTime.ParseExact(
                Gregorian.Date,
                Gregorian.Format.Replace('D', 'd').Replace('Y', 'y'),
                System.Globalization.CultureInfo.InvariantCulture);
    }

    public class DayData
    {
        public Times Timings { get; set; }
        public Date Date { get; set; }
        public Meta Meta { get; set; }
    }

    public class Request : IRequest
    {
        public DateTime Date { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public Method Method { get; set; }
        public bool GetEntireMonth { get; set; }

        public string GetUrl()
            => (GetEntireMonth
                ? $"http://api.aladhan.com/v1/calendarByCity/{Date.Year}/{Date.Month}"
                : $"http://api.aladhan.com/v1/timingsByCity/{Date:dd-MM-yyyy}")
            + $"?country={Country}"
            + $"&city={City}"
            + $"&method={(byte)Method}";
    }

    public class Response<T> where T : class
    {
        public int Code { get; set; }
        public string Status { get; set; }
        public T Data { get; set; }
    }

    public abstract class DataResponse<T> : Response<T> where T : class
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

    public class MonthResponse : DataResponse<DayData[]>
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

    public class ErrorResponse : Response<string>
    {

    }

    public enum Method : byte
    {
        // Values 0 and 7 are omitted, we don't support Shia

        [Description("University of Islamic Sciences, Karachi")]
        Karachi = 1,
        [Description("Islamic Society of North America")]
        ISNA = 2,
        [Description("Muslim World League")]
        MWL = 3,
        [Description("Umm Al-Qura University, Makkah")]
        Makkah = 4,
        [Description("Egyptian General Authority of Survey")]
        EGAS = 5,
        [Description("Gulf Region")]
        Gulf = 8,
        [Description("Kuwait")]
        Kuwait = 9,
        [Description("Qatar")]
        Qatar = 10,
        [Description("Majlis Ugama Islam Singapura, Singapore")]
        Singapore = 11,
        [Description("Union Organization Islamic de France")]
        UOIF = 12,
        [Description("Diyanet İşleri Başkanlığı, Turkey")]
        DIB = 13,
        [Description("Spiritual Administration of Muslims of Russia")]
        SAMR = 14,
        //[Description("Moonsighting Committee Worldwide (also requires shafaq parameter)")]
        //MCW = 15,
        [Description("Dubai (unofficial)")]
        Dubai = 16,
        [Description("Jabatan Kemajuan Islam Malaysia (JAKIM)")]
        JAKIM = 17,
        [Description("Tunisia")]
        TUNISIA = 18,
        [Description("Algeria")]
        ALGERIA = 19,
        [Description("Kementerian Agama Republik Indonesia")]
        KEMENAG = 20,
        [Description("Morocco")]
        MOROCCO = 21,
        [Description("Comunidade Islamica de Lisboa")]
        PORTUGAL = 22,
        [Description("Ministry of Awqaf, Islamic Affairs and Holy Places, Jordan")]
        JORDAN = 23,

        // Not ready yet
        //[Description("Custom Setting")]
        //Custom = 99
    }
}
