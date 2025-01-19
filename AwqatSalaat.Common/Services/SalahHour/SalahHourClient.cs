using AwqatSalaat.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AwqatSalaat.Services.SalahHour
{
    public class SalahHourClient : IServiceClient
    {
        public async Task<ServiceData> GetDataAsync(IRequest request)
        {
            var req = (SalahHourRequest)request;

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

        private static async Task<T> GetDataAsync<T>(IWebRequest request) where T : Response
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var httpResponse = await client.GetAsync(request.GetUrl());
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        string responseBody = await httpResponse.Content.ReadAsStringAsync();

                        if (string.IsNullOrEmpty(responseBody))
                        {
                            throw new SalahHourApiException("Islamic Finder service did not respond with data.");
                        }

                        T apiResponse = JsonConvert.DeserializeObject<T>(responseBody);

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
            }
            catch (HttpRequestException hre)
            {
                throw new NetworkException("Could not reach the server.", hre);
            }
        }
    }
}
