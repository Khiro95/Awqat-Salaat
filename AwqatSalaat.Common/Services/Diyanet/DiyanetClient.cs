using AwqatSalaat.Data;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AwqatSalaat.Services.Diyanet
{
    /// <summary>
    /// Official prayer times of the Presidency of Religious Affairs of Turkey (Diyanet), used by Turkish
    /// communities worldwide. Served without authentication by https://ezanvakti.emushaf.net, which mirrors
    /// Diyanet's data. Locations are Diyanet districts: country > city > district.
    /// </summary>
    public class DiyanetClient : IServiceClient
    {
        // Use a static HttpClient to avoid port-exhaustion problem
        private static readonly HttpClient _httpClient = new HttpClient();

        // The location lists rarely change, keep them for the whole session
        private static readonly ConcurrentDictionary<string, IList<DiyanetPlace>> _placesCache = new ConcurrentDictionary<string, IList<DiyanetPlace>>();

        // The API returns about 30 days from a few days ago, not whole months, so the widget
        // can't ask for "next month" and the calendar is not supported.
        public bool SupportMonthlyData => false;

        static DiyanetClient()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Awqat Salaat");
        }

        public async Task<ServiceData> GetDataAsync(IRequest request)
        {
            var req = (DiyanetRequest)request;
            Log.Debug("[Diyanet] Getting data for request: {@request}", req);

            if (req.DistrictId <= 0)
            {
                throw new ArgumentException("Invalid request. Diyanet service requires a district.");
            }

            var days = await GetAsync<DiyanetDay[]>(req.GetUrl()).ConfigureAwait(false);

            if (days is null || days.Length == 0)
            {
                throw new DiyanetApiException("No result");
            }

            return new ServiceData
            {
                Location = new Location { Country = req.CountryName, City = req.DistrictName },
                Times = BuildTimes(days, TimeZoneInfo.Local),
            };
        }

        internal static Dictionary<DateTime, PrayerTimes> BuildTimes(IEnumerable<DiyanetDay> days, TimeZoneInfo localTimeZone)
        {
            var dict = new Dictionary<DateTime, PrayerTimes>();

            foreach (var day in days)
            {
                var date = DateTime.ParseExact(day.Date, "dd.MM.yyyy", CultureInfo.InvariantCulture);
                // The day's UTC offset at the location (it follows the location's daylight saving time)
                var offset = DateTimeOffset.Parse(day.DateIso8601, CultureInfo.InvariantCulture).Offset;

                dict[date] = new PrayerTimes(new Dictionary<string, DateTime>
                {
                    [nameof(PrayerTimes.Fajr)] = ToLocal(date, day.Fajr, offset, localTimeZone),
                    [nameof(PrayerTimes.Shuruq)] = ToLocal(date, day.Shuruq, offset, localTimeZone),
                    [nameof(PrayerTimes.Dhuhr)] = ToLocal(date, day.Dhuhr, offset, localTimeZone),
                    [nameof(PrayerTimes.Asr)] = ToLocal(date, day.Asr, offset, localTimeZone),
                    [nameof(PrayerTimes.Maghrib)] = ToLocal(date, day.Maghrib, offset, localTimeZone),
                    [nameof(PrayerTimes.Isha)] = ToLocal(date, day.Isha, offset, localTimeZone),
                });
            }

            return dict;
        }

        private static DateTime ToLocal(DateTime date, string time, TimeSpan offset, TimeZoneInfo localTimeZone)
        {
            var clock = date + TimeSpan.ParseExact(time, @"hh\:mm", CultureInfo.InvariantCulture);

            // Most users pick their own place: keep the clock times as they are, which also stays right
            // on daylight saving days (the offset given is the one at midnight).
            if (localTimeZone.GetUtcOffset(date) == offset)
            {
                return clock;
            }

            var local = TimeZoneInfo.ConvertTime(new DateTimeOffset(clock, offset), localTimeZone);
            return DateTime.SpecifyKind(local.DateTime, DateTimeKind.Unspecified);
        }

        public static Task<IList<DiyanetPlace>> GetCountriesAsync()
            => GetPlacesAsync<DiyanetCountryDto>("ulkeler", d => new DiyanetPlace(d.Id, d.NameEn ?? d.Name));

        public static Task<IList<DiyanetPlace>> GetCitiesAsync(int countryId)
            => GetPlacesAsync<DiyanetCityDto>($"sehirler/{countryId}", d => new DiyanetPlace(d.Id, d.NameEn ?? d.Name));

        public static Task<IList<DiyanetPlace>> GetDistrictsAsync(int cityId)
            => GetPlacesAsync<DiyanetDistrictDto>($"ilceler/{cityId}", d => new DiyanetPlace(d.Id, d.NameEn ?? d.Name));

        private static async Task<IList<DiyanetPlace>> GetPlacesAsync<T>(string path, Func<T, DiyanetPlace> convert)
        {
            if (_placesCache.TryGetValue(path, out var cached))
            {
                return cached;
            }

            var items = await GetAsync<T[]>($"{DiyanetRequest.BaseUrl}/{path}").ConfigureAwait(false);
            IList<DiyanetPlace> places = (items ?? Array.Empty<T>())
                .Select(convert)
                .OrderBy(p => p.Name, StringComparer.CurrentCulture)
                .ToList();

            if (places.Count > 0)
            {
                _placesCache[path] = places;
            }

            return places;
        }

        private static async Task<T> GetAsync<T>(string url)
        {
            try
            {
                Log.Debug($"[Diyanet] Getting data from: {url}");
                var httpResponse = await _httpClient.GetAsync(url).ConfigureAwait(false);
                Log.Debug($"[Diyanet] Response status code: {httpResponse.StatusCode}");

                if (httpResponse.IsSuccessStatusCode)
                {
                    string responseBody = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<T>(responseBody);
                }

                // e.g. 400 or 502 for an unknown id
                throw new DiyanetApiException($"Something went wrong: {httpResponse.ReasonPhrase} (StatusCode={httpResponse.StatusCode})");
            }
            catch (HttpRequestException hre)
            {
                throw new NetworkException("Could not reach the server.", hre);
            }
        }
    }
}
