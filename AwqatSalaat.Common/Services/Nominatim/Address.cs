using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace AwqatSalaat.Services.Nominatim
{
    public class Address
    {
        [JsonProperty("city")]
        public string City { get; private set; }

        [JsonProperty("town")]
        public string Town { get; private set; }

        [JsonProperty("province")]
        public string Province { get; private set; }

        [JsonProperty("state")]
        public string State { get; private set; }

        [JsonProperty("country")]
        public string Country { get; private set; }

        [JsonProperty("country_code")]
        public string CountryCode { get; private set; }

        [OnDeserialized]
        private void CityFallback(StreamingContext context)
        {
            City = City ?? Province ?? Town;
        }
    }
}
