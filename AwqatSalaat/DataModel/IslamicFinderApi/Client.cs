using AwqatSalaat.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AwqatSalaat.DataModel.IslamicFinderApi
{
    public static class Client
    {
        public static async Task<SingleDayResponse> GetSingleDayDataAsync(string countryCode, string zipCode, Method method, DateTime? date = null)
        {
            Request request = new Request()
            {
                CountryCode = countryCode,
                ZipCode = zipCode,
                Method = (byte)method,
                Date = date ?? TimeStamp.Date,
                ShowEntireMonth = false
            };
            var res = await GetDataAsync<SingleDayResponse>(request);
            res.Times.Adjust(request.Date);
            return res;
        }

        public static async Task<EntireMonthResponse> GetEntireMonthDataAsync(string countryCode, string zipCode, Method method, DateTime? date = null)
        {
            Request request = new Request()
            {
                CountryCode = countryCode,
                ZipCode = zipCode,
                Method = (byte)method,
                Date = date ?? TimeStamp.Date,
                ShowEntireMonth = true
            };
            var res = await GetDataAsync<EntireMonthResponse>(request);
            return res;
        }

        private static async Task<T> GetDataAsync<T>(Request request) where T : Response
        {
            try
            {
                HttpClient client = new HttpClient();
                var httpResponse = await client.GetAsync(request.ToUrl());
                if (httpResponse.IsSuccessStatusCode)
                {
                    string responseBody = await httpResponse.Content.ReadAsStringAsync();
                    T apiResponse = JsonConvert.DeserializeObject<T>(responseBody);
                    if (!apiResponse.Success)
                    {
                        throw new IslamicFinderApiException(apiResponse.Message);
                    }
                    return apiResponse;
                }
                else
                {
                    throw new IslamicFinderApiException($"Something went wrong: {httpResponse.ReasonPhrase} (StatusCode={httpResponse.StatusCode})");
                }
            }
            catch (HttpRequestException hre)
            {
                throw new NetworkException("Could not reach the server.", hre);
            }
        }
    }
}
