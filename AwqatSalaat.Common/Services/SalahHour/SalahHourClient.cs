using AwqatSalaat.Data;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace AwqatSalaat.Services.SalahHour
{
    public class SalahHourClient : IServiceClient
    {
        public bool SupportMonthlyData => true;

        // Use a static HttpClient to avoid port-exhaustion problem
        private static readonly HttpClient _httpClient = new HttpClient();

        static SalahHourClient()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Awqat Salaat");
        }

        public async Task<ServiceData> GetDataAsync(IRequest request)
        {
            var req = (SalahHourRequest)request;
            Log.Debug("[Salah-Hour] Getting data for request: {@request}", req);

            if (req.GetEntireMonth)
            {
                var res = await GetDataAsync<EntireMonthResponse>(req);

                return new ServiceData
                {
                    Times = res.Times,
                    Location = res.Settings.Location
                };
            }
            else
            {
                var res = await GetDataAsync<SingleDayResponse>(req);
                res.Times.Adjust(request.Date);
                var dict = new Dictionary<DateTime, PrayerTimes>
                {
                    [request.Date] = res.Times,
                };

                return new ServiceData
                {
                    Times = dict,
                    Location = res.Settings.Location
                };
            }
        }

        internal static T Deserialize<T>(string json, DateTime requestDate) where T : Response
        {
            var settings = new JsonSerializerSettings
            {
                Context = new StreamingContext(StreamingContextStates.Other, requestDate),
            };

            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        private static async Task<T> GetDataAsync<T>(IWebRequest request) where T : Response
        {
            try
            {
                var url = request.GetUrl();
                Log.Debug($"[Salah-Hour] Getting data from: {url}");
                var httpResponse = await _httpClient.GetAsync(url);
                Log.Debug($"[Salah-Hour] Response status code: {httpResponse.StatusCode}");

                if (httpResponse.IsSuccessStatusCode)
                {
                    string responseBody = await httpResponse.Content.ReadAsStringAsync();

                    if (string.IsNullOrEmpty(responseBody))
                    {
                        throw new SalahHourApiException("Salah Hour service did not respond with data.");
                    }

                    T apiResponse = Deserialize<T>(responseBody, request.Date);

                    if (!apiResponse.Success)
                    {
                        throw new SalahHourApiException(apiResponse.Message);
                    }

                    return apiResponse;
                }
                else
                {
                    throw new SalahHourApiException($"Something went wrong: {httpResponse.ReasonPhrase} (StatusCode={httpResponse.StatusCode})");
                }
            }
            catch (HttpRequestException hre)
            {
                throw new NetworkException("Could not reach the server.", hre);
            }
        }
    }
}
