using AwqatSalaat.Data;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AwqatSalaat.Services.WaktuSolat
{
    /// <summary>
    /// Official JAKIM prayer times for Malaysia, served by https://api.waktusolat.app.
    /// Unlike calculation based services, the times come straight from JAKIM's e-Solat,
    /// so they match what mosques in Malaysia use (zone based, e.g. SGR01).
    /// </summary>
    public class WaktuSolatClient : IServiceClient
    {
        // Use a static HttpClient to avoid port-exhaustion problem
        private static readonly HttpClient _httpClient = new HttpClient();

        // Zone metadata rarely changes, keep it for the whole session
        private static readonly ConcurrentDictionary<string, WaktuSolatZone> _zoneCache = new ConcurrentDictionary<string, WaktuSolatZone>();

        public bool SupportMonthlyData => true;

        static WaktuSolatClient()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Awqat Salaat");
        }

        public async Task<ServiceData> GetDataAsync(IRequest request)
        {
            var req = (WaktuSolatRequest)request;
            Log.Debug("[WaktuSolat] Getting data for request: {@request}", req);

            if (!req.UseCoordinates || req.Latitude == default && req.Longitude == default)
            {
                throw new ArgumentException("Invalid request. Waktu Solat service require coordinates.");
            }

            var response = await GetAsync<WaktuSolatResponse>(req.GetUrl()).ConfigureAwait(false);

            if (response?.Prayers is null || response.Prayers.Length == 0)
            {
                throw new WaktuSolatApiException("No result");
            }

            var allTimes = BuildTimes(response);
            Dictionary<DateTime, PrayerTimes> times;

            if (req.GetEntireMonth)
            {
                times = allTimes;
            }
            else
            {
                if (!allTimes.TryGetValue(req.Date.Date, out var dayTimes))
                {
                    throw new WaktuSolatApiException($"No data for {req.Date:yyyy-MM-dd}");
                }

                times = new Dictionary<DateTime, PrayerTimes> { [req.Date.Date] = dayTimes };
            }

            var zone = await GetZoneAsync(response.Zone).ConfigureAwait(false);

            return new ServiceData
            {
                Location = new Location
                {
                    Country = "Malaysia",
                    City = string.IsNullOrEmpty(zone?.Negeri) ? response.Zone : $"{zone.Negeri} ({response.Zone})",
                },
                Times = times,
            };
        }

        private static Dictionary<DateTime, PrayerTimes> BuildTimes(WaktuSolatResponse response)
        {
            var dict = new Dictionary<DateTime, PrayerTimes>();

            foreach (var day in response.Prayers.OrderBy(p => p.Day))
            {
                var date = new DateTime(response.Year, response.MonthNumber, day.Day);
                var times = new PrayerTimes(new Dictionary<string, DateTime>
                {
                    [nameof(PrayerTimes.Fajr)]    = FromUnix(day.Fajr),
                    [nameof(PrayerTimes.Shuruq)]  = FromUnix(day.Syuruk),
                    [nameof(PrayerTimes.Dhuhr)]   = FromUnix(day.Dhuhr),
                    [nameof(PrayerTimes.Asr)]     = FromUnix(day.Asr),
                    [nameof(PrayerTimes.Maghrib)] = FromUnix(day.Maghrib),
                    [nameof(PrayerTimes.Isha)]    = FromUnix(day.Isha),
                });

                dict[date] = times;
            }

            return dict;
        }

        // Timestamps are absolute (UTC), so converting to the machine's local time zone gives
        // the correct wall clock time on a PC set to Malaysia time (UTC+8)
        private static DateTime FromUnix(long seconds)
            => DateTime.SpecifyKind(DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime, DateTimeKind.Unspecified);

        private static async Task<WaktuSolatZone> GetZoneAsync(string zoneCode)
        {
            if (string.IsNullOrEmpty(zoneCode))
            {
                return null;
            }

            if (_zoneCache.TryGetValue(zoneCode, out var cached))
            {
                return cached;
            }

            try
            {
                // Best effort, only used for the displayed location
                var zones = await GetAsync<WaktuSolatZone[]>($"{WaktuSolatRequest.BaseUrl}/zones/{zoneCode}").ConfigureAwait(false);
                var zone = zones?.FirstOrDefault();

                if (zone != null)
                {
                    _zoneCache[zoneCode] = zone;
                }

                return zone;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"[WaktuSolat] Could not get zone info for {zoneCode}");
                return null;
            }
        }

        private static async Task<T> GetAsync<T>(string url)
        {
            try
            {
                Log.Debug($"[WaktuSolat] Getting data from: {url}");
                var httpResponse = await _httpClient.GetAsync(url).ConfigureAwait(false);
                Log.Debug($"[WaktuSolat] Response status code: {httpResponse.StatusCode}");
                string responseBody = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (httpResponse.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<T>(responseBody);
                }

                string message = null;

                try
                {
                    message = JsonConvert.DeserializeObject<WaktuSolatError>(responseBody)?.Message;
                }
                catch
                {
                    // Not a JSON error body
                }

                // e.g. 422 "No zone found for the given coordinates." when outside Malaysia
                throw new WaktuSolatApiException(message ?? $"Something went wrong: {httpResponse.ReasonPhrase} (StatusCode={httpResponse.StatusCode})");
            }
            catch (HttpRequestException hre)
            {
                throw new NetworkException("Could not reach the server.", hre);
            }
        }
    }
}
