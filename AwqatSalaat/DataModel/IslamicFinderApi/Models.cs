using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace AwqatSalaat.DataModel.IslamicFinderApi
{
    public class PrayerTimes : IEnumerable<KeyValuePair<string, DateTime>>
    {
        [JsonProperty]
        private readonly Dictionary<string, DateTime> times;

        public DateTime Fajr => GetTime();
        public DateTime Dhuhr => GetTime();
        public DateTime Asr => GetTime();
        public DateTime Maghrib => GetTime();
        public DateTime Isha => GetTime();

        public PrayerTimes(Dictionary<string, DateTime> times) => this.times = times;

        public void Adjust(DateTime day)
        {
            foreach (var time in times)
            {
                times[time.Key] = day.Date + time.Value.TimeOfDay;
            }
        }

        public IEnumerator<KeyValuePair<string, DateTime>> GetEnumerator() => times.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private DateTime GetTime([System.Runtime.CompilerServices.CallerMemberName] string name = null) => times[name];
    }

    public class Request
    {
        public string CountryCode { get; set; }
        public string ZipCode { get; set; }
        public byte Method { get; set; }
        public bool ShowEntireMonth { get; set; }
        public DateTime Date { get; set; }

        public string ToUrl()
            => "http://www.islamicfinder.us/index.php/api/prayer_times"
            + $"?country={CountryCode}"
            + $"&zipcode={ZipCode}"
            + $"&method={Method}"
            + $"&show_entire_month={ShowEntireMonth}"
            + $"&date={Date:yyyy-MM-dd}"
            + $"&time_format=0";
    }

    public abstract class Response
    {
        protected TimeZoneInfo timeZone;

        public bool Success;
        public string Message;
        public Settings Settings;

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

    public class SingleDayResponse : Response
    {
        public Dictionary<string, string> Results;
        public PrayerTimes Times;

        protected sealed override void ParseImpl()
        {
            DateTime today = DateTime.Today;
            DateTime date = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, DateTimeKind.Unspecified);
            Dictionary<string, DateTime> dayTimes = new Dictionary<string, DateTime>(5);
            foreach (var time in Results)
            {
                if (time.Key != "Duha")
                {
                    dayTimes.Add(time.Key, ParseTimeToLocal(time.Value, date));
                }
            }
            Times = new PrayerTimes(dayTimes);
        }
    }

    public class EntireMonthResponse : Response
    {
        public Dictionary<string, Dictionary<string, string>> Results;
        public Dictionary<DateTime, PrayerTimes> Times;

        protected sealed override void ParseImpl()
        {
            Times = new Dictionary<DateTime, PrayerTimes>(Results.Count);
            foreach (var day in Results)
            {
                DateTime date = DateTime.Parse(day.Key, System.Globalization.CultureInfo.InvariantCulture);
                Dictionary<string, DateTime> dayTimes = new Dictionary<string, DateTime>(5);
                foreach (var time in day.Value)
                {
                    if (time.Key != "Duha")
                    {
                        dayTimes.Add(time.Key, ParseTimeToLocal(time.Value, date));
                    }
                }
                Times.Add(date, new PrayerTimes(dayTimes));
            }
        }
    }

    public class Settings
    {
        public Method Method { get; set; } = Method.MWL;
        public string TimeZone { get; set; }
        public Location Location { get; set; }
    }

    public class Location
    {
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
    }

    public enum Method : byte
    {
        // Values 0 and 7 are omitted, we don't support Shia

        [Description("Karachi - University of Islamic Sciences")]
        Karachi = 1,
        [Description("ISNA - Islamic Society of North America")]
        ISNA = 2,
        [Description("MWL - Muslim World League")]
        MWL = 3,
        [Description("Mecca - Umm al-Qura")]
        UAQ = 4,
        [Description("Egyptian General Authority of Survey")]
        EGAS = 5,
        // Not ready yet
        //[Description("Custom Setting")]
        //Custom = 6,
        [Description("Algerian Minister of Religious Affairs and Wakfs")]
        AMRAW = 8,
        [Description("Gulf 90 Minutes Fixed Isha")]
        Gulf90 = 9,
        [Description("Egyptian General Authority of Survey (Bis)")]
        EGAS2 = 10,
        [Description("UOIF - Union Des Organisations Islamiques De France")]
        UOIF = 11,
        [Description("Sistem Informasi Hisab Rukyat Indonesia")]
        SIHRI = 12,
        [Description("Diyanet İşleri Başkanlığı")]
        DIB = 13,
        [Description("Germany Custom")]
        GerCustom = 14,
        [Description("Russia Custom")]
        RusCustom = 15,
    }
}
