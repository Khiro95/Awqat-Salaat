using Newtonsoft.Json;

namespace AwqatSalaat.Services.Nominatim
{
    public class Place
    {
        [JsonProperty("lat")]
        public decimal Latitude { get; private set; }

        [JsonProperty("lon")]
        public decimal Longitude { get; private set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; private set; }

        [JsonProperty("address")]
        public Address Address { get; private set; }
    }
}
