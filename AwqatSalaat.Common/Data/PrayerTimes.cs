using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AwqatSalaat.Data
{
    public class PrayerTimes : IEnumerable<KeyValuePair<string, DateTime>>
    {
        [JsonProperty]
        private readonly Dictionary<string, DateTime> times;

        public DateTime Fajr => GetTime();
        public DateTime Shuruq => GetTime();
        public DateTime Dhuhr => GetTime();
        public DateTime Asr => GetTime();
        public DateTime Maghrib => GetTime();
        public DateTime Isha => GetTime();

        [JsonConstructor]
        public PrayerTimes(IEnumerable<KeyValuePair<string, DateTime>> times) => this.times = times.ToDictionary(kv => kv.Key, kv => kv.Value);
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
}