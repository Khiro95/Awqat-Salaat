using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AwqatSalaat.Services.Nominatim
{
    public class NominatimClient
    {
        // Use a static HttpClient to avoid port-exhaustion problem
        private static readonly HttpClient _httpClient = new HttpClient();

        static NominatimClient()
        {
            _httpClient.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Awqat Salaat");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en");
        }

        public static async Task<Place[]> Search(string query, CancellationToken cancellationToken)
        {
            string path = $"search?q={Uri.EscapeDataString(query)}&format=json&addressdetails=1&dedupe=1&limit=50";

            var httpResponse = await _httpClient.GetAsync(path, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            if (httpResponse.IsSuccessStatusCode)
            {
                string responseBody = await httpResponse.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<Place[]>(responseBody);
            }
            else
            {
                throw new NominatimException(httpResponse.ReasonPhrase);
            }
        }

        public static async Task<Place> Reverse(decimal latitude, decimal longitude, CancellationToken cancellationToken)
        {
            string lat = latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string lon = longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string path = $"reverse?lat={lat}&lon={lon}&format=json&addressdetails=1&zoom=10";

            var httpResponse = await _httpClient.GetAsync(path, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            if (httpResponse.IsSuccessStatusCode)
            {
                string responseBody = await httpResponse.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<Place>(responseBody);
            }
            else
            {
                throw new NominatimException(httpResponse.ReasonPhrase);
            }
        }
    }
}
