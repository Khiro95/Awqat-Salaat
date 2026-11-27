using Newtonsoft.Json;

namespace AwqatSalaat.Services.Diyanet
{
    /// <summary>One day of times, in the location's own time zone.</summary>
    internal class DiyanetDay
    {
        // dd.MM.yyyy
        [JsonProperty("MiladiTarihKisa")]
        public string Date { get; set; }

        // Midnight of the day with the location's UTC offset, e.g. 2026-11-15T00:00:00.0000000+01:00
        [JsonProperty("MiladiTarihUzunIso8601")]
        public string DateIso8601 { get; set; }

        [JsonProperty("Imsak")]
        public string Fajr { get; set; }

        [JsonProperty("Gunes")]
        public string Shuruq { get; set; }

        [JsonProperty("Ogle")]
        public string Dhuhr { get; set; }

        [JsonProperty("Ikindi")]
        public string Asr { get; set; }

        [JsonProperty("Aksam")]
        public string Maghrib { get; set; }

        [JsonProperty("Yatsi")]
        public string Isha { get; set; }
    }

    /// <summary>A country, city (şehir) or district (ilçe) in Diyanet's location tree.</summary>
    public class DiyanetPlace
    {
        public int Id { get; }
        public string Name { get; }

        public DiyanetPlace(int id, string name)
        {
            Id = id;
            Name = name?.Trim();
        }

        public override string ToString() => Name;
    }

    internal class DiyanetCountryDto
    {
        [JsonProperty("UlkeID")] public int Id { get; set; }
        [JsonProperty("UlkeAdi")] public string Name { get; set; }
        [JsonProperty("UlkeAdiEn")] public string NameEn { get; set; }
    }

    internal class DiyanetCityDto
    {
        [JsonProperty("SehirID")] public int Id { get; set; }
        [JsonProperty("SehirAdi")] public string Name { get; set; }
        [JsonProperty("SehirAdiEn")] public string NameEn { get; set; }
    }

    internal class DiyanetDistrictDto
    {
        [JsonProperty("IlceID")] public int Id { get; set; }
        [JsonProperty("IlceAdi")] public string Name { get; set; }
        [JsonProperty("IlceAdiEn")] public string NameEn { get; set; }
    }
}
