using Newtonsoft.Json;

namespace AwqatSalaat.Services.GitHub
{
    internal class GitHubError
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
