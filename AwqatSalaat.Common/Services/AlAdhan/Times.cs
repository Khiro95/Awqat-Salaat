using Newtonsoft.Json;
using System.Collections.Generic;

namespace AwqatSalaat.Services.AlAdhan
{
    internal class Times
    {
        public string Fajr { get; set; }
        [JsonProperty("Sunrise")]
        public string Shuruq { get; set; }
        public string Dhuhr { get; set; }
        public string Asr { get; set; }
        public string Maghrib { get; set; }
        public string Isha { get; set; }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            yield return new KeyValuePair<string, string>(nameof(Fajr), Fajr);
            yield return new KeyValuePair<string, string>(nameof(Shuruq), Shuruq);
            yield return new KeyValuePair<string, string>(nameof(Dhuhr), Dhuhr);
            yield return new KeyValuePair<string, string>(nameof(Asr), Asr);
            yield return new KeyValuePair<string, string>(nameof(Maghrib), Maghrib);
            yield return new KeyValuePair<string, string>(nameof(Isha), Isha);
        }
    }
}
