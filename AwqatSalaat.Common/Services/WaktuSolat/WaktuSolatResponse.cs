using Newtonsoft.Json;

namespace AwqatSalaat.Services.WaktuSolat
{
    public class WaktuSolatResponse
    {
        [JsonProperty("zone")]
        public string Zone { get; set; }

        [JsonProperty("year")]
        public int Year { get; set; }

        [JsonProperty("month_number")]
        public int MonthNumber { get; set; }

        [JsonProperty("prayers")]
        public WaktuSolatDay[] Prayers { get; set; }
    }

    public class WaktuSolatDay
    {
        [JsonProperty("day")]
        public int Day { get; set; }

        [JsonProperty("hijri")]
        public string Hijri { get; set; }

        // All times are Unix timestamps (seconds, UTC)
        [JsonProperty("imsak")]
        public long Imsak { get; set; }

        [JsonProperty("fajr")]
        public long Fajr { get; set; }

        [JsonProperty("syuruk")]
        public long Syuruk { get; set; }

        [JsonProperty("dhuha")]
        public long Dhuha { get; set; }

        [JsonProperty("dhuhr")]
        public long Dhuhr { get; set; }

        [JsonProperty("asr")]
        public long Asr { get; set; }

        [JsonProperty("maghrib")]
        public long Maghrib { get; set; }

        [JsonProperty("isha")]
        public long Isha { get; set; }
    }

    public class WaktuSolatZone
    {
        [JsonProperty("jakimCode")]
        public string JakimCode { get; set; }

        [JsonProperty("negeri")]
        public string Negeri { get; set; }

        [JsonProperty("daerah")]
        public string Daerah { get; set; }
    }

    internal class WaktuSolatError
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
