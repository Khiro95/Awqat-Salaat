using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AwqatSalaat.Services.GitHub
{
    internal class GitHubClient
    {
        // Use a static HttpClient to avoid port-exhaustion problem
        private static readonly HttpClient _httpClient = new HttpClient();

        static GitHubClient()
        {
            _httpClient.BaseAddress = new Uri("https://api.github.com/repos/Khiro95/Awqat-Salaat/");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Awqat Salaat");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        }

        public static async Task<Release[]> GetReleases(CancellationToken cancellationToken = default)
        {
            var httpResponse = await _httpClient.GetAsync("releases", cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            if (httpResponse.IsSuccessStatusCode)
            {
                string responseBody = await httpResponse.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<Release[]>(responseBody);
            }
            else
            {
                GitHubError error = null;

                try
                {
                    string responseBody = await httpResponse.Content.ReadAsStringAsync();

                    error = JsonConvert.DeserializeObject<GitHubError>(responseBody);
                }
                catch (Exception ex)
                {
#if DEBUG
                    throw;
#endif
                }

                throw new Exception($"GitHub Error: {error?.Message ?? httpResponse.StatusCode.ToString()}");
            }
        }

        public static async Task<Release> GetLatestRelease(CancellationToken cancellationToken = default)
        {
            var httpResponse = await _httpClient.GetAsync("releases/latest", cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            if (httpResponse.IsSuccessStatusCode)
            {
                string responseBody = await httpResponse.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<Release>(responseBody);
            }
            else
            {
                GitHubError error = null;

                try
                {
                    string responseBody = await httpResponse.Content.ReadAsStringAsync();

                    error = JsonConvert.DeserializeObject<GitHubError>(responseBody);
                }
                catch (Exception ex)
                {
#if DEBUG
                    throw;
#endif
                }

                throw new Exception($"GitHub Error: {error?.Message ?? httpResponse.StatusCode.ToString()}");
            }
        }
    }
}
